using Sekiro.BehaviourMachine;
using Sekiro.BehaviourTree.Utils;
using Sekiro.Enemy.Setting;
using UnityEngine;

namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 敌人行为树条件检查类
    /// 提供所有用于行为树的条件判断方法
    ///
    /// 距离判定已委托给 DistanceUtility 静态工具类，
    /// 消除与 EnemyBehaviourMachine.GetDistanceRange() 的代码重复。
    /// </summary>
    public class EnemyConditions
    {
        private readonly BehaviourTree _tree;

        // 缓存常用的链式访问以避免重复的 .BehaviourMachine.Variable/Setting 调用
        private EnemyBehaviourMachine _machine;
        private EnemyVariable _variable;
        private EnemyConfigSO _enemyConfig;

        /// <summary>
        /// 构造函数，传入所属的行为树实例
        /// </summary>
        public EnemyConditions(BehaviourTree tree)
        {
            _tree = tree;
            RefreshCache();
        }

        /// <summary>
        /// 刷新链式访问缓存（在 BehaviourTree 初始化后调用）
        /// 如果 BehaviourMachine 尚未就绪，缓存保持为 null，
        /// 后续调用方法时会自动重试获取（延迟初始化）。
        /// </summary>
        public void RefreshCache()
        {
            if (_tree == null) return;

            var machine = _tree.BehaviourMachine;
            if (machine == null) return;

            _machine = machine;
            _variable = machine.Variable;
            _enemyConfig = machine.EnemyConfig;
        }

        /// <summary>
        /// 延迟初始化 — 如果缓存尚未就绪，尝试从 _tree 获取
        /// </summary>
        private void EnsureCache()
        {
            if (_machine == null || _variable == null || _enemyConfig == null)
            {
                RefreshCache();
            }
        }

        /// <summary>
        /// 判断是否"超出"指定距离范围（使用 > 边界）
        /// 委托给 DistanceUtility 统一实现
        /// </summary>
        public bool IsAtRange(DistanceRange range)
        {
            EnsureCache();
            if (_variable == null || _enemyConfig == null)
            {
                return false;
            }
            return DistanceUtility.IsAtRange(_variable.CachedDistance, range, _enemyConfig);
        }

        /// <summary>
        /// 判断是否"在"指定距离范围内（使用 <= 边界）
        /// 委托给 DistanceUtility 统一实现
        /// </summary>
        public bool IsInRange(DistanceRange range)
        {
            EnsureCache();
            if (_variable == null || _enemyConfig == null)
            {
                return false;
            }
            return DistanceUtility.IsInRange(_variable.CachedDistance, range, _enemyConfig);
        }

        /// <summary>
        /// 检查上一个行为是否是指定行为
        /// </summary>
        public bool LastBehaviourIs(EnemyBehaviour behaviour)
        {
            EnsureCache();
            if (_machine == null)
            {
                return false;
            }
            return _machine.LastBehaviour == behaviour;
        }
    }
}
