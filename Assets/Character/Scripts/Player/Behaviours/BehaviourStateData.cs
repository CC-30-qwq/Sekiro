using Sekiro.Character.Data;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 行为运行时状态数据（值类型，零GC分配）
    /// 从 PlayerBaseBehaviour 提取，集中管理 ConditionEvaluator 所需的运行时状态字段，
    /// 减少 PlayerBaseBehaviour 的字段膨胀，同时提供统一的调试/序列化入口。
    /// </summary>
    internal struct BehaviourStateData
    {
        /// <summary>冲刺松开计时器 (Sprint/Start_Sprint 设置)</summary>
        internal float SprintReleaseTimerValue;

        /// <summary>冲刺键按住计时器 (Dodge 设置)</summary>
        internal float SprintHoldTimerValue;

        /// <summary>当前攻击阶段 (GroundAttackBehaviour 设置)</summary>
        internal AttackState CurrentAttackStateValue;

        /// <summary>当前招架阶段 (ParryBehaviour 设置)</summary>
        internal ParryState CurrentParryStateValue;

        /// <summary>是否正在攻击中 (GroundAttackBehaviour 设置)</summary>
        internal bool IsAttackingValue;

        /// <summary>是否正在招架中 (ParryBehaviour 设置)</summary>
        internal bool IsParryingValue;

        /// <summary>是否左转 (GroundAttackBehaviour 设置)</summary>
        internal bool TurnIsLeft;

        /// <summary>是否右转 (GroundAttackBehaviour 设置)</summary>
        internal bool TurnIsRight;

        /// <summary>
        /// 重置所有状态为默认值 — 在行为 OnEnter 时调用，消除手动逐个重置
        /// </summary>
        internal void Reset()
        {
            SprintReleaseTimerValue = 0f;
            SprintHoldTimerValue = 0f;
            CurrentAttackStateValue = AttackState.Charging;
            CurrentParryStateValue = ParryState.Windup;
            IsAttackingValue = false;
            IsParryingValue = false;
            TurnIsLeft = false;
            TurnIsRight = false;
        }
    }
}
