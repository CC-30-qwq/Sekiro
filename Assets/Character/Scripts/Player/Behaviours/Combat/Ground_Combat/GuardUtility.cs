using System;
using System.Collections.Generic;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 防御/格挡共享逻辑工具类 — 消除 DefenceBehaviour 与 Block 之间的代码重叠
    /// 
    /// 两者在以下逻辑上完全一致：
    /// 1. DamageResistanceModifier 设置/清除（OnEnter/OnExit）
    /// 2. 受击状态转换规则（IsHit → Block/Hit）
    /// </summary>
    public static class GuardUtility
    {
        /// <summary>
        /// 应用防御/格挡伤害抗性（OnEnter 时调用）
        /// </summary>
        /// <param name="health">角色 Health 组件</param>
        /// <param name="resistance">抗性值（0~1）</param>
        public static void ApplyGuardResistance(Health health, float resistance)
        {
            if (health != null)
                health.DamageResistanceModifier = resistance;
        }

        /// <summary>
        /// 清除防御/格挡伤害抗性（OnExit 时调用）
        /// </summary>
        /// <param name="health">角色 Health 组件</param>
        public static void ClearGuardResistance(Health health)
        {
            if (health != null)
                health.DamageResistanceModifier = 0f;
        }

        /// <summary>
        /// 构建防御/格挡通用受击状态转换规则
        /// 规则：
        /// - 被击中且方向非向下 → Block
        /// - 被击中且方向向下 → Hit
        /// </summary>
        /// <param name="rules">规则列表</param>
        /// <param name="behaviour">当前行为实例（提供 IsHit/LastDamageInfo/Methods/Machine 访问）</param>
        public static void BuildGuardTransitionRules(List<(Func<bool>, Action)> rules, PlayerBaseBehaviour behaviour)
        {
            rules.Add((() => behaviour.IsHit && behaviour.Methods.GetHitDirection(behaviour.LastDamageInfo.Direction, behaviour.Machine.transform) != Vector2.down,
                () => behaviour.Machine.TryChangeState(PlayerBehaviour.Block)));
            rules.Add((() => behaviour.IsHit && behaviour.Methods.GetHitDirection(behaviour.LastDamageInfo.Direction, behaviour.Machine.transform) == Vector2.down,
                () => behaviour.Machine.TryChangeState(PlayerBehaviour.Hit)));
        }
    }
}
