using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 输入类型枚举
    /// </summary>
    public enum InputType { Attack = 0, Parry = 1, Jump = 2, Dodge = 3 }

    /// <summary>
    /// 输入工具组件，管理玩家输入和缓冲
    /// </summary>
    public class InputsUtility : MonoBehaviour, IInputStateSink
    {
        #region Events

        /// <summary>锁定键按下事件（用于 PlayerCamera 等外部组件）</summary>
        public event Action OnLockPressed;

        #endregion

        #region Public Fields

        public Vector2 MoveInput;
        public bool AttackInput;
        public bool ChargeInput;
        public bool ParryInput;
        public bool DefenceInput;
        public bool JumpInput;
        public bool DodgeInput;
        public bool SprintInput;
        public bool LockInput;

        #endregion

        #region Private Fields

        public PlayerInput PlayerInput { get; private set; }
        public InputBuffer Buffer { get; private set; }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 统一 PlayerInput 初始化 — 单点创建，仅在需要时初始化一次
        /// 消除 Awake/OnEnable/OnDisable 多次调用可能导致的内存泄漏
        /// </summary>
        private void EnsurePlayerInputInitialized()
        {
            if (PlayerInput == null)
                PlayerInput = new PlayerInput();
        }

        private void Awake()
        {
            Buffer = GetComponent<InputBuffer>();
        }

        private void OnEnable()
        {
            EnsurePlayerInputInitialized();
            PlayerInput.Enable();
        }

        private void OnDisable()
        {
            if (PlayerInput != null)
            {
                PlayerInput.Disable();
            }
        }

        private void OnDestroy()
        {
            if (PlayerInput != null)
            {
                PlayerInput.Dispose();
                PlayerInput = null;
            }
        }


        private void Update()
        {
            var inputs = PlayerInput.PlayerInputs;

            ChargeInput = inputs.Attack.IsPressed();
            DefenceInput = inputs.Defence.IsPressed();
            SprintInput = inputs.Sprint.IsPressed();
            MoveInput = inputs.Move.ReadValue<Vector2>();

            ProcessBufferedInput(inputs.Attack, InputType.Attack);
            ProcessBufferedInput(inputs.Jump, InputType.Jump);
            // Defence输入映射为Parry类型（格挡动作），Sprint映射为Dodge（闪避动作）
            ProcessBufferedInput(inputs.Defence, InputType.Parry);
            ProcessBufferedInput(inputs.Sprint, InputType.Dodge);

            bool lockPressed = inputs.Lock.WasPressedThisDynamicUpdate();
            LockInput = lockPressed;
            if (lockPressed)
                OnLockPressed?.Invoke();
        }

        #endregion

        #region Input Rate Limiting

        /// <summary>
        /// [安全] 当前帧已处理的输入事件数 — 用于速率限制，防止输入缓冲区被高频轰炸
        /// </summary>
        private int _inputsThisFrame;

        /// <summary>
        /// [安全] 检查输入速率是否超过上限，超限时记录警告并拒绝
        /// </summary>
        private bool TryAcceptInput()
        {
            if (_inputsThisFrame >= Constants.MaxInputsPerFrame)
            {
                return false; // 超过安全上限，静默拒绝
            }
            _inputsThisFrame++;
            return true;
        }

        private void LateUpdate()
        {
            // 帧末重置输入计数
            _inputsThisFrame = 0;
        }

        #endregion

        #region Public Methods

        public bool OutputIns(InputType input)
        {
            if (Buffer != null && !Buffer.IsEmpty && Buffer.TryPeekType(out var peekType) && input == peekType)
            {
                Buffer.ConsumeBuffer();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 非破坏性输入检测 - 仅检查输入类型是否匹配，不消耗缓冲区
        /// 用于条件评估阶段，避免输入在 AND 链后续条件失败后被浪费
        /// </summary>
        public bool PeekIns(InputType input)
        {
            return Buffer != null && !Buffer.IsEmpty && Buffer.TryPeekType(out var peekType) && input == peekType;
        }

        /// <summary>
        /// 消耗缓冲区中最旧的输入（供 Action 阶段使用）
        /// 在条件评估通过后，实际执行转换时调用
        /// 不检查类型，仅消耗队首元素（PeekIns 已确保类型匹配过）
        /// </summary>
        public void ConsumeInputBuffer()
        {
            if (Buffer != null && !Buffer.IsEmpty)
            {
                Buffer.ConsumeBuffer();
            }
        }

        /// <summary>
        /// 设置指定输入类型的标志状态（供 IInputStateSink 实现）
        /// </summary>
        void IInputStateSink.SetInputFlag(InputType type, bool value)
        {
            switch (type)
            {
                case InputType.Attack:
                    AttackInput = value;
                    break;
                case InputType.Jump:
                    JumpInput = value;
                    break;
                case InputType.Parry:
                    ParryInput = value;
                    break;
                case InputType.Dodge:
                    DodgeInput = value;
                    break;
            }
        }

        /// <summary>
        /// 清除所有输入标志（供 IInputStateSink 实现）
        /// </summary>
        void IInputStateSink.ClearAllInputFlags()
        {
            AttackInput = false;
            JumpInput = false;
            ParryInput = false;
            DodgeInput = false;
        }

        /// <summary>
        /// 重置所有输入状态
        /// </summary>
        public void ResetInputs()
        {
            MoveInput = Vector2.zero;
            AttackInput = false;
            ChargeInput = false;
            ParryInput = false;
            DefenceInput = false;
            JumpInput = false;
            DodgeInput = false;
            SprintInput = false;
            Buffer?.ClearBuffer();
        }

        #endregion

        #region Private Methods

        private void ProcessBufferedInput(InputAction action, InputType type)
        {
            if (action.WasPressedThisDynamicUpdate() && Buffer != null)
            {
                if (TryAcceptInput())
                {
                    Buffer.BufferInput(type);
                }
            }
        }

        #endregion
    }
}
