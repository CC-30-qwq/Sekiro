using Sekiro.BehaviourMachine;
using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.Combat

{
    /// <summary>
    /// 攻击阶段驱动器 - 值类型结构体，零GC分配
    /// 封装攻击阶段时序管理（Charging → Windup → Attacking → Recovery → Exit），
    /// 消除玩家/敌人攻击阶段代码重复。
    /// 
    /// 使用方式：
    ///   - 玩家/敌人攻击：统一调用 Initialize(data)，始终从 Charging 阶段开始
    ///   - 如需跳过 Charging（敌人），在 ComboConfigSO 中设置 ChargeTime = 0 即可
    ///   - 每帧调用 Advance(data) 推进阶段
    /// </summary>
    public struct AttackPhaseDriver
    {
        #region State

        private AttackState _currentState;
        private float _phaseTimer;
        private float _phaseDuration;
        private float _phaseTimeOffset;
        private float _totalTime;

        // 预计算逆值（初始化时计算，避免每帧除法）
        private float _invPhaseDuration;
        private float _invTotalTime;

        #endregion

        #region Read-Only Properties

        /// <summary>当前攻击阶段</summary>
        public readonly AttackState CurrentState => _currentState;

        /// <summary>当前阶段已耗时（秒）</summary>
        public readonly float CurrentPhaseTime => _phaseTimer;

        /// <summary>从行为 Initialize 起的总消逝时间（秒）</summary>
        public readonly float TotalElapsedTime => _phaseTimeOffset + _phaseTimer;

        /// <summary>当前阶段总时长（秒）</summary>
        public readonly float CurrentPhaseDuration => _phaseDuration;

        /// <summary>是否处于活跃攻击状态（Charging ~ Recovery，以及 Exit 缓冲期内）</summary>
        public readonly bool IsAttacking => _currentState != AttackState.Exit || _phaseTimer < _phaseDuration;

        /// <summary>是否允许切换状态（Exit 缓冲结束）</summary>
        public readonly bool CanChangeState => _currentState == AttackState.Exit;

        #endregion

        #region Progress Properties

        /// <summary>当前阶段进度 [0, 1]</summary>
        public float StateProgress { get; private set; }

        /// <summary>总进度 [0, 1]（从 Charging/Windup 起算）</summary>
        public float TotalProgress { get; private set; }

        #endregion

        #region Initialization

        /// <summary>
        /// 预计算逆值（初始化/阶段切换时调用）
        /// </summary>
        private void UpdateInverses()
        {
            _invPhaseDuration = _phaseDuration > 0f ? 1f / _phaseDuration : 0f;
            _invTotalTime = _totalTime > 0f ? 1f / _totalTime : 0f;
        }

        /// <summary>
        /// 初始化攻击阶段驱动器，始终从 Charging 阶段开始。
        /// 若某阶段时长为 0，则自动跳过该阶段。
        /// </summary>
        public void Initialize(in AttackData data)
        {
            _phaseTimer = 0f;
            _phaseDuration = data.ChargeTime;
            _phaseTimeOffset = 0f;
            _totalTime = data.TotalTime;
            _currentState = AttackState.Charging;
            StateProgress = 0f;
            TotalProgress = 0f;
            UpdateInverses();

            // 若 Charging 阶段时长为 0，立即跳过到下一个非零阶段
            while (_phaseDuration == 0f && _currentState != AttackState.Exit)
            {
                AdvanceToNextPhase(data);
            }
        }

        #endregion

        #region Phase Advancement

        /// <summary>
        /// 推进到下一阶段（内部方法）
        /// 重置计时器，更新状态、时长和时间偏移
        /// </summary>
        private void AdvanceToNextPhase(in AttackData data)
        {
            _phaseTimer = 0f;
            StateProgress = 1f;

            switch (_currentState)
            {
                case AttackState.Charging:
                    _currentState = AttackState.Windup;
                    _phaseDuration = data.WindupTime;
                    _phaseTimeOffset = data.ChargeTime;
                    break;

                case AttackState.Windup:
                    _currentState = AttackState.Attacking;
                    _phaseDuration = data.AttackDuration;
                    _phaseTimeOffset = data.ChargeTime + data.WindupTime;
                    break;

                case AttackState.Attacking:
                    _currentState = AttackState.Recovery;
                    _phaseDuration = data.RecoveryTime;
                    _phaseTimeOffset = data.ChargeTime + data.WindupTime + data.AttackDuration;
                    break;

                case AttackState.Recovery:
                    _currentState = AttackState.Exit;
                    _phaseDuration = data.ExitTime;
                    _phaseTimeOffset = data.ChargeTime + data.WindupTime + data.AttackDuration + data.RecoveryTime;
                    break;

                case AttackState.Exit:
                    StateProgress = 1f;
                    TotalProgress = 1f;
                    _phaseDuration = 0f; // [修复] 标记攻击序列完全结束，使 IsAttacking 正确返回 false
                    return;
            }

            UpdateInverses();
        }

        /// <summary>
        /// 推进阶段时序（每帧调用）
        /// Charging → Windup → Attacking → Recovery → Exit
        /// 若某阶段时长为 0，自动跳过该阶段
        /// 零GC：乘法替代除法，使用预计算的逆值
        /// </summary>
        public void Advance(in AttackData data)
        {
            // [修复] 攻击序列已完全结束（Exit 计时器走完后 _phaseDuration 已被设为 0），
            // 不再推进，此时 IsAttacking 返回 false，NotAttacking 条件可正常触发
            if (_currentState == AttackState.Exit && _phaseDuration == 0f)
                return;

            _phaseTimer += Time.deltaTime;

            // 计算进度（使用预计算逆值，避免每帧除法）
            if (_totalTime > 0f)
            {
                StateProgress = _phaseTimer * _invPhaseDuration;
                TotalProgress = (_phaseTimeOffset + _phaseTimer) * _invTotalTime;
            }

            // 检查是否应当推进到下一阶段
            if (_phaseTimer >= _phaseDuration)
            {
                AdvanceToNextPhase(data);

                // 持续跳过时长为 0 的阶段（如 WindupTime=0，则从 Charging 直接到 Attacking）
                // 注意：Exit 阶段被标记为 0 时长后不会再进入此循环（_currentState == Exit 时条件不满足）
                while (_phaseDuration == 0f && _currentState != AttackState.Exit)
                {
                    AdvanceToNextPhase(data);
                }
            }
        }

        #endregion
    }
}
