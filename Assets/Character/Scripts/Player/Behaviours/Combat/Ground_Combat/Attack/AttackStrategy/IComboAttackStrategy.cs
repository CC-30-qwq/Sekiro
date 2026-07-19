using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 连击攻击策略接口 — 消除 AttackGround.OnUpdate 中基于 AttackType 的 switch 分支
    /// 
    /// 每个 AttackType 实现各自的阶段行为逻辑：
    /// - ChargeAttackStrategy: 蓄力攻击（Charging 检测松手释放）
    /// - MeleeAttackStrategy: 近战连击（直接触发）
    /// - SprintAttackStrategy: 冲刺攻击（Charging 松手标记 → Windup 释放）
    /// </summary>
    public interface IComboAttackStrategy
    {
        /// <summary>
        /// 每帧更新攻击逻辑
        /// </summary>
        /// <param name="behaviour">所属 GroundAttackBehaviour 实例（提供 PhaseDriver/Inputs/Machine 等访问）</param>
        /// <param name="currentCombo">当前连击配置</param>
        /// <param name="attackDirection">攻击方向（可修改，由策略更新朝向）</param>
        /// <param name="needsComboInit">是否需要重建连击（策略可设为 true 触发连击切换）</param>
        /// <param name="hasRelease">松手标记（SprintAttack 专用）</param>
        void OnUpdate(
            GroundAttackBehaviour behaviour,
            ComboConfigSO currentCombo,
            ref Vector3 attackDirection,
            ref bool needsComboInit,
            ref bool hasRelease);
    }
}
