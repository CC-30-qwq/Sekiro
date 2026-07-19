using System.Collections.Generic;
using Sekiro.Character.Data;

namespace Sekiro.Enemy.Behaviours
{
    /// <summary>
    /// 敌人攻击策略工厂 — 管理 AttackType 到 IEnemyAttackStrategy 的映射
    /// 
    /// 新增 AttackType 时，只需：
    /// 1. 创建新的 IEnemyAttackStrategy 实现
    /// 2. 在此工厂的静态构造函数中注册
    /// 无需修改 Enemy_AttackBehaviour
    /// </summary>
    public static class EnemyAttackStrategyFactory
    {
        private static readonly Dictionary<AttackType, IEnemyAttackStrategy> _strategies;

        static EnemyAttackStrategyFactory()
        {
            _strategies = new Dictionary<AttackType, IEnemyAttackStrategy>
            {
                { AttackType.MeleeAttack, new EnemyMeleeAttackStrategy() },
                { AttackType.RangedAttack, new EnemyRangedAttackStrategy() },
            };
        }

        /// <summary>
        /// 获取指定攻击类型的策略实例
        /// </summary>
        /// <param name="attackType">攻击类型</param>
        /// <returns>策略实例，未注册时返回 null</returns>
        public static IEnemyAttackStrategy GetStrategy(AttackType attackType)
        {
            _strategies.TryGetValue(attackType, out var strategy);
            return strategy;
        }

        /// <summary>
        /// 注册自定义策略（扩展点，用于外部注册新攻击类型）
        /// </summary>
        public static void RegisterStrategy(AttackType attackType, IEnemyAttackStrategy strategy)
        {
            _strategies[attackType] = strategy;
        }
    }
}
