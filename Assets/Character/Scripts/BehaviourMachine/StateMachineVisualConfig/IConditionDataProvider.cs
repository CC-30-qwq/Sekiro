using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 条件评估数据提供者接口
    /// ConditionEvaluator 通过此接口读取运行时数据，消除对 PlayerBaseBehaviour 的具体类型依赖。
    /// 
    /// [设计目标]
    /// - 解耦：条件评估系统不依赖具体 Behaviour 类型
    /// - 可测试：可通过 mock 接口实现进行单元测试
    /// - 可扩展：Enemy 或其他角色类型实现此接口后即可复用 ConditionEvaluator
    /// 
    /// [职责划分]
    /// - Input Layer：输入状态查询和缓冲区控制
    /// - Variable Layer：角色运行时变量（移动/跳跃/动画参数）
    /// - Behaviour State Fields：行为实例设置的瞬时状态（攻击阶段/转向标志等）
    /// </summary>
    public interface IConditionDataProvider
    {
        // ======== Input Layer ========

        /// <summary>非破坏性输入检测 — 检查输入缓冲区是否匹配指定类型但不消耗</summary>
        bool PeekIns(InputType input);

        /// <summary>消耗输入缓冲区中最旧的输入（Action 阶段使用）</summary>
        void ConsumeInputBuffer();

        /// <summary>防御键是否按住</summary>
        bool DefenceInput { get; }

        /// <summary>冲刺键是否按住</summary>
        bool SprintInput { get; }



        // ======== Variable Layer (Read-only for conditions) ========

        /// <summary>行为计时器（进入当前状态后经过的时间）</summary>
        float Timer { get; }

        /// <summary>角色是否站在地面上</summary>
        bool IsGrounded { get; }

        /// <summary>目标移动方向向量（XZ平面）</summary>
        Vector2 TargetVector { get; }

        /// <summary>归一化后的动画输入值</summary>
        Vector2 AnimInputValue { get; }

        /// <summary>垂直速度</summary>
        float VerticalSpeed { get; }

        /// <summary>跳跃方向（XZ平面）</summary>
        Vector2 JumpDirection { get; }


        // ======== Variable Layer (Read-write for actions) ========

        /// <summary>当前连击配置</summary>
        ComboConfigSO CurrentCombo { get; set; }

        /// <summary>水平方向速度/力量值</summary>
        float HorizontalSpeed { get; set; }


        // ======== Behaviour State Fields (set by behaviour instances) ========

        /// <summary>冲刺松开计时器</summary>
        float SprintReleaseTimerValue { get; }

        /// <summary>冲刺键按住计时器</summary>
        float SprintHoldTimerValue { get; }

        /// <summary>是否正在攻击中</summary>
        bool IsAttackingValue { get; }

        /// <summary>当前攻击阶段</summary>
        AttackState CurrentAttackStateValue { get; }

        /// <summary>当前招架阶段</summary>
        ParryState CurrentParryStateValue { get; }

        /// <summary>是否正在招架中</summary>
        bool IsParryingValue { get; }

        /// <summary>是否左转</summary>
        bool TurnIsLeft { get; }

        /// <summary>是否右转</summary>
        bool TurnIsRight { get; }
    }
}
