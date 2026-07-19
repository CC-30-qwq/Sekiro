using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.Enemy.Behaviours
{
    /// <summary>
    /// 敌人攻击策略接口 — 消除 Enemy_AttackBehaviour.OnUpdate 中基于 AttackType 的 switch 分支
    /// 
    /// 每个 AttackType 实现各自的 Recovery 阶段逻辑：
    /// - EnemyMeleeAttackStrategy: 近战攻击（Windup 设置方向 → Recovery 切连击）
    /// - EnemyRangedAttackStrategy: 远程攻击（Attacking 设方向 → Recovery 切连击）
    /// </summary>
    public interface IEnemyAttackStrategy
    {
        /// <summary>
        /// 每帧更新攻击逻辑
        /// </summary>
        /// <param name="behaviour">所属 Enemy_AttackBehaviour 实例</param>
        /// <param name="phaseDriver">攻击阶段驱动器</param>
        /// <param name="currentCombo">当前连击配置</param>
        /// <param name="attackDirection">攻击方向（可修改）</param>
        /// <param name="needsComboInit">是否需要重建连击（策略可设为 true 触发连击切换）</param>
        void OnUpdate(
            Enemy_AttackBehaviour behaviour,
            ref AttackPhaseDriver phaseDriver,
            ComboConfigSO currentCombo,
            ref Vector3 attackDirection,
            ref bool needsComboInit);
    }
}
