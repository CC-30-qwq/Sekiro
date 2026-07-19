using System.Collections.Generic;
using Sekiro.Character.Data;
using Sekiro.Combat;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 连击攻击策略工厂 — 管理 AttackType 到 IComboAttackStrategy 的映射
    /// 
    /// 新增 AttackType 时，只需：
    /// 1. 创建新的 IComboAttackStrategy 实现
    /// 2. 在此工厂的静态构造函数中注册
    /// 无需修改 AttackGround 或其他已有策略
    /// </summary>
    public static class ComboAttackStrategyFactory
    {
        private static readonly Dictionary<AttackType, IComboAttackStrategy> _strategies;

        static ComboAttackStrategyFactory()
        {
            _strategies = new Dictionary<AttackType, IComboAttackStrategy>
            {
                { AttackType.ChargeAttack, new ChargeAttackStrategy() },
                { AttackType.MeleeAttack, new MeleeAttackStrategy() },
                { AttackType.SprintAttack, new SprintAttackStrategy() },
                { AttackType.RangedAttack, new RangedAttackStrategy() },
            };
        }

        /// <summary>
        /// 获取指定攻击类型的策略实例
        /// </summary>
        /// <param name="attackType">攻击类型</param>
        /// <returns>策略实例，未注册时返回 null</returns>
        public static IComboAttackStrategy GetStrategy(AttackType attackType)
        {
            _strategies.TryGetValue(attackType, out var strategy);
            return strategy;
        }

        /// <summary>
        /// 注册自定义策略（扩展点，用于外部注册新攻击类型）
        /// </summary>
        public static void RegisterStrategy(AttackType attackType, IComboAttackStrategy strategy)
        {
            _strategies[attackType] = strategy;
        }
    }
}
