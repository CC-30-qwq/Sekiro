using Sekiro.BehaviourMachine;
using UnityEngine;

namespace Sekiro.Combat
{
    /// <summary>
    /// 招架阶段驱动器 — 值类型结构体，零GC分配
    /// 仿照 AttackPhaseDriver 设计，封装招架三段时序管理（Windup → Parrying → Recovery），
    /// 消除 ParryBehaviour 中与 AttackPhaseDriver 重复的阶段计时/推进逻辑。
    /// 
    /// 使用方式：
    ///   - 在 ParryBehaviour.OnEnter 中调用 Initialize(windupTime, parryDuration, recoveryTime)
    ///   - 每帧调用 Advance() 推进阶段
    ///   - 仅用于 ParryBehaviour，消除手写计时器的重复代码
    /// </summary>
    public struct ParryPhaseDriver
    {
        #region State

        private ParryState _currentPhase;
        private float _phaseTimer;

        // 三段时长（秒）
        private float _windupTime;
        private float _parryDuration;
        private float _recoveryTime;

        // 三段总时长（秒，用于总进度计算）
        private float _totalDuration;

        /// <summary>是否已初始化（Initialize 调用后为 true）</summary>
        private bool _initialized;

        /// <summary>上次推进时的阶段，用于检测阶段变化</summary>
        private ParryState _previousPhase;

        #endregion

        #region Read-Only Properties

        /// <summary>当前招架阶段</summary>
        public readonly ParryState CurrentPhase => _currentPhase;

        /// <summary>当前阶段已耗时（秒）</summary>
        public readonly float PhaseTimer => _phaseTimer;

        /// <summary>是否已完成全部阶段（Exit + Recovery 结束）</summary>
        public readonly bool IsComplete => _initialized && _currentPhase == ParryState.Recovery
                                            && _phaseTimer >= _recoveryTime;

        /// <summary>当前阶段是否发生了切换（Advance 调用后检查）</summary>
        public readonly bool HasPhaseChanged => _currentPhase != _previousPhase;

        #endregion

        #region Progress Properties

        /// <summary>当前阶段进度 [0, 1]</summary>
        public float PhaseProgress { get; private set; }

        /// <summary>总进度 [0, 1]（从 Windup 开始到 Recovery 结束）</summary>
        public float TotalProgress { get; private set; }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化招架三段时序，在 ParryBehaviour.OnEnter 中调用。
        /// 时序流程：Windup → Parrying → Recovery
        /// </summary>
        /// <param name="windupTime">起手时长（秒）</param>
        /// <param name="parryDuration">招架窗口时长（秒）</param>
        /// <param name="recoveryTime">收招时长（秒）</param>
        public void Initialize(float windupTime, float parryDuration, float recoveryTime)
        {
            _currentPhase = ParryState.Windup;  // 枚举起始值为 Parrying
            _previousPhase = _currentPhase;
            _phaseTimer = 0f;
            _windupTime = windupTime;
            _parryDuration = parryDuration;
            _recoveryTime = recoveryTime;
            _totalDuration = windupTime + parryDuration + recoveryTime;
            _initialized = true;
            PhaseProgress = 0f;
            TotalProgress = 0f;
        }

        #endregion

        #region Phase Advancement

        /// <summary>
        /// 推进招架阶段时序（每帧在 ParryBehaviour.OnUpdate 中调用）
        /// Windup → Parrying → Recovery
        /// </summary>
        public void Advance()
        {
            if (!_initialized) return;

            _previousPhase = _currentPhase;
            _phaseTimer += Time.deltaTime;

            // 计算进度
            float currentDuration = _currentPhase switch
            {
                ParryState.Windup => _windupTime,
                ParryState.Parrying => _parryDuration,
                ParryState.Recovery => _recoveryTime,
                _ => 1f,
            };

            PhaseProgress = currentDuration > 0f ? Mathf.Clamp01(_phaseTimer / currentDuration) : 1f;
            TotalProgress = _totalDuration > 0f
                ? Mathf.Clamp01(GetTotalElapsedTime() / _totalDuration)
                : 1f;

            // 检查阶段切换（包含零时长阶段跳过：如果当前阶段时长为0，立即推进到下一阶段）
            // [安全] 使用 while 循环替代递归调用，防止零时长阶段链导致栈溢出
            while (_phaseTimer >= currentDuration && _currentPhase != ParryState.Recovery)
            {
                switch (_currentPhase)
                {
                    case ParryState.Windup:
                        _phaseTimer = 0f;
                        _currentPhase = ParryState.Parrying;
                        break;

                    case ParryState.Parrying:
                        _phaseTimer = 0f;
                        _currentPhase = ParryState.Recovery;
                        break;

                    case ParryState.Recovery:
                        // Recovery 是最后一个阶段，不重置 _phaseTimer
                        // 让计时器继续累加，确保 IsComplete 能正确返回 true
                        break;
                }

                // 重新计算新阶段的持续时间
                // 如果新阶段时长为零，while 循环会继续推进到下一阶段
                currentDuration = _currentPhase switch
                {
                    ParryState.Parrying => _parryDuration,
                    ParryState.Recovery => _recoveryTime,
                    _ => 1f,
                };
            }
        }

        /// <summary>
        /// 重置驱动器状态 — 在 ParryBehaviour.OnExit 中调用
        /// </summary>
        public void Reset()
        {
            _initialized = false;
            _currentPhase = ParryState.Windup;
            _phaseTimer = 0f;
            PhaseProgress = 0f;
            TotalProgress = 0f;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// 获取从 Windup 开始的总消逝时间（秒）
        /// </summary>
        private float GetTotalElapsedTime()
        {
            float elapsed = _phaseTimer;
            if (_currentPhase >= ParryState.Parrying) elapsed += _windupTime;
            if (_currentPhase >= ParryState.Recovery) elapsed += _parryDuration;
            return elapsed;
        }

        #endregion
    }
}
