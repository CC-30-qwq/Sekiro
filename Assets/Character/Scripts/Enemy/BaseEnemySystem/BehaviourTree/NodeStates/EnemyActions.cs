using Sekiro.BehaviourMachine;
using UnityEngine;

namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 敌人行为树动作执行类
    /// 提供所有用于行为树的动作执行方法
    /// </summary>
    public class EnemyActions
    {
        private readonly BehaviourTree _tree;

        /// <summary>
        /// 构造函数，传入所属的行为树实例
        /// </summary>
        public EnemyActions(BehaviourTree tree)
        {
            _tree = tree;
        }

        public void ChangeTo(EnemyBehaviour behaviour)
        {
            _tree.BehaviourMachine.TryChangeState(behaviour);
        }
    }
}
