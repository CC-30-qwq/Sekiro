using Cinemachine;
using Sekiro.BehaviourMachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private PlayerVision _vision;
    [SerializeField] private InputsUtility _input;
    [SerializeField] private PlayerBehaviourMachine _playerMachine;

    [Header("Free Look Settings")]
    [SerializeField] private bool _enableRecenter = true;
    [SerializeField] private float _recenterWaitTime = 0.5f;
    [SerializeField] private float _recenterTime = 1f;

    [Header("Lock-On Settings")]
    [SerializeField] private float _povSmoothSpeed = 5f;
    [SerializeField] private float _yFrame = 0f;

	[Header("Target Switching")]
	[SerializeField] private float _switchCooldown = 0.2f;
	[SerializeField] private float _switchThreshold = 2f;
	[SerializeField] private float _switchDecayRate = 5f;

    #endregion

    #region Private Fields

    private CinemachineVirtualCamera _cinemachine;
    private CinemachinePOV _pov;
    private Transform _camTransform;

    private Transform _lockedTarget;
    private Transform _lockedHitPoint;

    private bool _lockPressedFlag;
    private bool _isLocked;
    private bool _previousLockState;

    private float _originalMaxSpeed;
    private string _originalHorizontalAxis;
    private string _originalVerticalAxis;

    private float _lastSwitchTime;
    private float _horizVelocity;
    private float _vertVelocity;

    private Vector2 _switchAccumulator;

    #endregion

    #region Public Properties

    public bool IsLocked => _isLocked;
    public Transform LockedTarget => _lockedTarget;

    #endregion

    #region Unity Lifecycle

    private void Awake() => InitializeComponents();

    private void Start()
    {
        if (!TryCacheOriginalPovSettings())
        {
            Debug.LogError("[PlayerCamera] ❌ CinemachinePOV 组件缺失，锁定功能不可用");
            enabled = false;
            return;
        }
        SubscribeToInput();
    }

    private void OnEnable() => SubscribeToInput();
    private void OnDisable() => UnsubscribeFromInput();

    private void Update()
    {
        UpdateLockState();
        UpdateRecenterPerFrame();
        HandleTargetSwitching();
    }

    private void FixedUpdate()
    {
        if (_isLocked && _lockedHitPoint != null && _pov != null)
            SmoothLookAt(_lockedHitPoint);
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        _cinemachine = GetComponentInChildren<CinemachineVirtualCamera>();

        if (_cinemachine != null)
        {
            _pov = _cinemachine.GetCinemachineComponent<CinemachinePOV>();
            _camTransform = _cinemachine.transform;
        }

        if (_vision == null)
            _vision = GetComponentInChildren<PlayerVision>();
    }

    private void SubscribeToInput()
    {
        if (_input == null)
        {
            Debug.LogError("[PlayerCamera] ❌ _input 为空！请在 Inspector 中拖拽 InputsUtility 引用");
            return;
        }
        _input.OnLockPressed -= OnLockPressedHandler;
        _input.OnLockPressed += OnLockPressedHandler;
    }

    private void UnsubscribeFromInput()
    {
        if (_input != null)
            _input.OnLockPressed -= OnLockPressedHandler;
    }

    private void OnLockPressedHandler() => _lockPressedFlag = true;

    /// <returns>如果 POV 组件可用返回 true</returns>
    private bool TryCacheOriginalPovSettings()
    {
        if (_pov == null) return false;

        _originalMaxSpeed = _pov.m_HorizontalAxis.m_MaxSpeed;
        _originalHorizontalAxis = _pov.m_HorizontalAxis.m_InputAxisName;
        _originalVerticalAxis = _pov.m_VerticalAxis.m_InputAxisName;
        return true;
    }

    #endregion

    #region Lock State Management

    private void UpdateLockState()
    {
        bool hasBestTarget = _vision != null && _vision.BestTarget != null;

        bool lockPressed = _lockPressedFlag;
        _lockPressedFlag = false;

        if (lockPressed && hasBestTarget)
        {
            _vision.ForceScan();

            var best = _vision.BestTarget;
            if (best != null)
            {
                _lockedTarget = best;
                _lockedHitPoint = GetHitPoint(_lockedTarget);
                _isLocked = !_isLocked;
                ResetLockSmoothDamp();
            }
        }

        if (!hasBestTarget && _isLocked)
            _isLocked = false;

        // 当锁定目标从可见列表中丢失时，自动切换到新的最佳目标
        if (_isLocked && _lockedTarget != null && HasVision() && _vision.GetTargetIndex(_lockedTarget) < 0)
        {
            var fallbackTarget = _vision.BestTarget;
            if (fallbackTarget != null)
            {
                _lockedTarget = fallbackTarget;
                _lockedHitPoint = GetHitPoint(_lockedTarget);
                ResetLockSmoothDamp();
            }
            else
            {
                _isLocked = false;
            }
        }

        // 将锁定目标同步到 CharacterVariable.Target
        SyncTargetToCharacterVariable();

        // 锁定状态变化时，切换 POV 输入
        if (_isLocked != _previousLockState)
        {
            SetPovInputEnabled(!_isLocked);

            if (!_isLocked)
            {
                _lockedTarget = null;
                _lockedHitPoint = null;
                ResetLockSmoothDamp();
            }

            _previousLockState = _isLocked;
        }
    }

    /// <summary>
    /// 每帧更新 Recentering 状态
    /// 
    /// Recentering 激活条件（全部满足）：
    ///   1. 不在锁定状态
    ///   2. _enableRecenter == true
    ///   3. 玩家正在移动（MoveInput.sqrMagnitude > 0）
    /// 
    /// 其他情况：禁用 Recentering（锁定时或静止时或 _enableRecenter=false）
    /// </summary>
    private void UpdateRecenterPerFrame()
    {
        if (_pov == null) return;

        if (_enableRecenter && !_isLocked)
        {
            bool isMoving = _input != null && _input.MoveInput.sqrMagnitude > 0f;
            _pov.m_HorizontalRecentering.m_enabled = isMoving;
            _pov.m_VerticalRecentering.m_enabled = isMoving;

            if (isMoving)
            {
                _pov.m_HorizontalRecentering.m_WaitTime = _recenterWaitTime;
                _pov.m_VerticalRecentering.m_WaitTime = _recenterWaitTime;
                _pov.m_HorizontalRecentering.m_RecenteringTime = _recenterTime;
                _pov.m_VerticalRecentering.m_RecenteringTime = _recenterTime;
            }
        }
        else
        {
            _pov.m_HorizontalRecentering.m_enabled = false;
            _pov.m_VerticalRecentering.m_enabled = false;
        }
    }

    #endregion

    #region Target Switching

    private void HandleTargetSwitching()
    {
        if (!_isLocked || !HasVision() || _vision.VisibleTargets.Count <= 1)
        {
            _switchAccumulator = Vector2.zero;
            return;
        }

        if (Time.time - _lastSwitchTime < _switchCooldown)
            return;

        // 累积累加：将每帧鼠标 delta 累加到 _switchAccumulator
        Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        if (delta.sqrMagnitude > 0.001f)
        {
            _switchAccumulator += delta;
        }
        else
        {
            // 无输入时快速衰减，避免上一次残留累积导致误触发
            _switchAccumulator = Vector2.Lerp(_switchAccumulator, Vector2.zero, _switchDecayRate * Time.deltaTime);
            if (_switchAccumulator.sqrMagnitude < 0.01f)
                _switchAccumulator = Vector2.zero;
        }

        float absX = Mathf.Abs(_switchAccumulator.x);
        float absY = Mathf.Abs(_switchAccumulator.y);

        // 累积值未达到阈值 → 不触发切换
        if (absX < _switchThreshold && absY < _switchThreshold)
            return;

        int currentIndex = _vision.GetTargetIndex(_lockedTarget);
        if (currentIndex < 0)
        {
            _switchAccumulator = Vector2.zero;
            return;
        }

        int count = _vision.VisibleTargets.Count;
        int nextIndex = currentIndex;

        if (absX >= absY && absX >= _switchThreshold)
        {
            // 水平方向：左右循环切换
            nextIndex = _switchAccumulator.x > 0
                ? (currentIndex + 1) % count
                : (currentIndex - 1 + count) % count;
        }
        else if (absY >= _switchThreshold)
        {
            // 垂直方向：上下循环切换（按排序跳转 = 远近/优先级切换）
            int step = _switchAccumulator.y > 0 ? -1 : 1; // 上推 = 选择列表中更靠前的（优先级更高）
            nextIndex = (currentIndex + step + count) % count;
        }

        if (nextIndex != currentIndex)
        {
            ApplySwitchTarget(nextIndex);
            // 触发切换后立即重置累加器，防止连续触发
            _switchAccumulator = Vector2.zero;
        }
    }

    private void ApplySwitchTarget(int nextIndex)
    {
        if (!HasVision()) return;

        var nextTarget = _vision.VisibleTargets[nextIndex];
        _lockedTarget = nextTarget.Root;
        _lockedHitPoint = nextTarget.HitPoint;
        _lastSwitchTime = Time.time;
        ResetLockSmoothDamp();
    }

    #endregion

    #region Camera Behavior

    private void SmoothLookAt(Transform target)
    {
        if (_pov == null || target == null) return;

        float smoothTime = GetSmoothDampTime();
        Vector3 camPos = _camTransform != null ? _camTransform.position : transform.position;

        // 水平角度（围绕 Y 轴）
        float desiredHoriz = Mathf.Atan2(
            target.position.x - camPos.x,
            target.position.z - camPos.z) * Mathf.Rad2Deg;

        _pov.m_HorizontalAxis.Value = Mathf.SmoothDampAngle(
            _pov.m_HorizontalAxis.Value, desiredHoriz,
            ref _horizVelocity, smoothTime);

        // 垂直角度（围绕 X 轴）— 使用水平距离计算更精确的俯仰角
        Vector3 toTarget = target.position - camPos;
        float horizontalDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
        if (horizontalDist < 0.001f) return; // 防止除零或极小距离导致抖动

        float targetAngleY = Mathf.Atan2(toTarget.y, horizontalDist) * Mathf.Rad2Deg;
        float desiredVert = _yFrame - targetAngleY;

        _pov.m_VerticalAxis.Value = Mathf.SmoothDampAngle(
            _pov.m_VerticalAxis.Value, desiredVert,
            ref _vertVelocity, smoothTime);
    }

    #endregion

    #region Utility

    private Transform GetHitPoint(Transform enemy)
    {
        if (enemy == null || !HasVision()) return null;

        int index = _vision.GetTargetIndex(enemy);
        if (index >= 0)
            return _vision.VisibleTargets[index].HitPoint;

        return null;
    }

    /// <summary>
    /// 将目标同步到 CharacterVariable.Target，
    /// 供 BehaviourMachine 中的 Behaviour 通过 Variable.Target 访问锁定目标
    /// 
    /// - 锁定时 → 使用锁定目标 _lockedTarget
    /// - 非锁定但视野内有敌人 → 使用 PlayerVision 最近的可视目标
    /// - 无目标 → null
    /// </summary>
    private void SyncTargetToCharacterVariable()
    {
        if (_playerMachine == null) return;

        if (_isLocked && _lockedTarget != null)
        {
            _playerMachine.Variable.Target = _lockedTarget;
        }
        else if (HasVision() && _vision.BestTarget != null)
        {
            _playerMachine.Variable.Target = _vision.BestTarget;
        }
        else
        {
            _playerMachine.Variable.Target = null;
        }
    }

    private bool HasVision() => _vision != null;

    private void ResetLockSmoothDamp()
    {
        _horizVelocity = 0f;
        _vertVelocity = 0f;
    }

    private void SetPovInputEnabled(bool enabled)
    {
        if (_pov == null) return;

        _pov.m_HorizontalAxis.m_MaxSpeed = enabled ? _originalMaxSpeed : 0f;
        _pov.m_VerticalAxis.m_MaxSpeed = enabled ? _originalMaxSpeed : 0f;
        _pov.m_HorizontalAxis.m_InputAxisName = enabled ? _originalHorizontalAxis : null;
        _pov.m_VerticalAxis.m_InputAxisName = enabled ? _originalVerticalAxis : null;
    }

    private float GetSmoothDampTime()
    {
        return _povSmoothSpeed > 0.001f ? 1f / _povSmoothSpeed : 1f;
    }

    #endregion
}
