using System.Collections.Generic;
using Sekiro.Combat;
using UnityEngine;

/// <summary>
/// 玩家视觉检测系统（重构版）
/// 
/// 设计思路：
/// - 【编排器模式】PlayerVision 作为检测管道的编排器，不负责具体逻辑
/// - 【职责分离】将物理扫描 → 过滤 → 可见性检查 → 评分 → 排序拆分为独立服务
/// - 【IDetectable】通过接口解耦对 Health/CharacterTarget 的硬编码依赖
/// - 【可扩展】新增可检测实体只需实现 IDetectable，无需修改检测管道
/// - 【Zero-Alloc】复用 NonAlloc API + 对象池，无运行时 GC 分配
/// 
/// 管道流程：
///   TargetDetector (OverlapSphere) 
///     → IDetectable 适配器 
///       → TargetVisibilityChecker (视野锥角 + 视锥 + 遮挡) 
///         → TargetScorer (评分) 
///           → _sortedBuffer (排序) 
///             → _visibleTargets (结果)
/// </summary>
public class PlayerVision : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _barrierLayer;
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Detection Settings")]
    [SerializeField] private float _detectionRadius = 15f;
    [SerializeField] private float _scanInterval = 0.1f;
    [SerializeField] private float _maxViewAngle = 60f;

    [Header("Target Scoring")]
    [SerializeField] private float _screenCenterWeight = 1.0f;
    [SerializeField] private float _distanceWeight = 0.5f;

    [Header("Performance")]
    [SerializeField] private int _initialBufferSize = 32;

    #endregion

    #region Private Fields

    // — 检测管道服务 —
    private TargetDetector _detector;
    private TargetVisibilityChecker _visibilityChecker;
    private TargetScorer _scorer;

    // — 检测结果 —
    private readonly List<DetectableTarget> _visibleTargets = new();
    private readonly List<DetectableTarget> _sortedBuffer = new();

    // — 缓存 —
    private Transform _transform;
    private float _lastScanTime;

    // — 运行时诊断数据 —
    private int _diagTotalCandidates;
    private int _diagNoDetectable;
    private int _diagNotDetectable;
    private int _diagNotVisible;
    private int _diagPassed;

    #endregion

    #region Public Properties

    /// <summary>当前可见目标列表（按评分排序）</summary>
    public IReadOnlyList<DetectableTarget> VisibleTargets => _visibleTargets;

    /// <summary>当前最优目标</summary>
    public Transform BestTarget => _visibleTargets.Count > 0 ? _visibleTargets[0].Root : null;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _transform = transform;

        if (_camera == null)
            _camera = Camera.main;

        // [安全] Camera.main 为 null 时禁用组件，避免后续 NRE
        if (_camera == null)
        {
            Debug.LogError("[PlayerVision] 场景中找不到 MainCamera 标签的摄像机，禁用组件", this);
            enabled = false;
            return;
        }

        if (_enemyLayer == 0)
            _enemyLayer = LayerMask.GetMask("Default");

        // 初始化检测管道服务
        _detector = new TargetDetector(_transform, _detectionRadius, _enemyLayer, _initialBufferSize);
        _visibilityChecker = new TargetVisibilityChecker(_camera, _maxViewAngle, _barrierLayer);
        _scorer = new TargetScorer(_camera, _screenCenterWeight, _distanceWeight);
    }

    private void Update()
    {
        if (Time.time - _lastScanTime >= _scanInterval)
        {
            ScanForTargets();
            _lastScanTime = Time.time;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 获取目标在可见列表中的索引
    /// </summary>
    public int GetTargetIndex(Transform target)
    {
        for (int i = 0; i < _visibleTargets.Count; i++)
        {
            if (_visibleTargets[i].Root == target)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 强制立即执行一次扫描
    /// </summary>
    public void ForceScan()
    {
        ScanForTargets();
        _lastScanTime = Time.time;
    }

    #endregion

    #region Core Detection Pipeline

    /// <summary>
    /// 执行一次完整的检测管道
    /// 
    /// 管道步骤：
    ///   1. TargetDetector — 物理扫描收集候选
    ///   2. IDetectable 适配 — 获取可检测接口
    ///   3. IsDetectable — 存活/可用性检查
    ///   4. TargetVisibilityChecker — 可见性检查（视野锥角+视锥+遮挡）
    ///   5. TargetScorer — 评分
    ///   6. 排序 — 按分数升序排列
    /// </summary>
    private void ScanForTargets()
    {
        _sortedBuffer.Clear();
        ResetDiagnostics();

        // Step 1: 物理扫描
        int hitCount = _detector.CollectCandidates();

        // Step 2-4: 逐个检测过滤
        for (int i = 0; i < hitCount; i++)
        {
            var candidate = _detector.GetCandidate(i);
            var rootTransform = candidate.transform;

            if (rootTransform == _transform) continue;

            _diagTotalCandidates++;

            // Step 2: 获取 IDetectable 适配器
            if (!rootTransform.TryGetComponent<IDetectable>(out var detectable))
            {
                _diagNoDetectable++;
                continue;
            }

            // Step 3: 检测是否可检测（存活等）
            if (!detectable.IsDetectable)
            {
                _diagNotDetectable++;
                continue;
            }

            Transform detectPoint = detectable.DetectPoint;

            // Step 4: 可见性检查
            if (!_visibilityChecker.IsVisible(detectPoint))
            {
                _diagNotVisible++;
                continue;
            }

            // Step 5: 评分
            _diagPassed++;
            float score = _scorer.CalculateScore(_transform.position, detectPoint, detectable);
            _sortedBuffer.Add(new DetectableTarget(rootTransform, detectPoint, score));
        }

        // Step 6: 排序（分数越低优先级越高）
        _sortedBuffer.Sort((a, b) => a.Score.CompareTo(b.Score));

        // 替换结果列表
        _visibleTargets.Clear();
        _visibleTargets.AddRange(_sortedBuffer);

        // 自动诊断
        AutoDiagnose();
    }

    #endregion

    #region Diagnostics

    private void ResetDiagnostics()
    {
        _diagTotalCandidates = 0;
        _diagNoDetectable = 0;
        _diagNotDetectable = 0;
        _diagNotVisible = 0;
        _diagPassed = 0;

        _visibilityChecker.ResetDiagnostics();
    }

    /// <summary>
    /// 当范围内有候选但全部被筛掉时，打印诊断信息
    /// </summary>
    private void AutoDiagnose()
    {
        if (_diagTotalCandidates <= 0 || _diagPassed > 0) return;
        if (Time.frameCount % 10 != 0) return;

        Debug.Log(
            $"[PlayerVision] ⚠ 扫描到 {_diagTotalCandidates} 个候选，0 个通过:\n" +
            $"  ▸ 无 IDetectable 组件: {_diagNoDetectable}\n" +
            $"  ▸ 不可检测(已死亡/禁用): {_diagNotDetectable}\n" +
            $"  ▸ 可见性检查失败(视野角:{_visibilityChecker.DiagOutOfViewAngle} + " +
            $"视锥:{_visibilityChecker.DiagOutOfFrustum} + 遮挡:{_visibilityChecker.DiagOccluded}): {_diagNotVisible}\n" +
            $"  ▸ 通过: {_diagPassed}\n" +
            $"  💡 提示: 确保敌人挂载了 EnemyDetectable 组件，且子物体中有 Renderer 和 Health 组件"
        );
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);

        if (!Application.isPlaying) return;
        if (_visibleTargets == null || _visibleTargets.Count == 0) return;

        for (int i = 0; i < _visibleTargets.Count; i++)
        {
            Gizmos.color = i == 0 ? Color.red : Color.yellow;
            Gizmos.DrawLine(transform.position, _visibleTargets[i].HitPoint.position);
        }
    }

    #endregion
}
