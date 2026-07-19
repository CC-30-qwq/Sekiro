using Sekiro.BehaviourMachine;
using UnityEngine;

namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 行为树基类，AI角色的行为树系统
    /// </summary>
    [RequireComponent(typeof(EnemyBehaviourMachine))]
    public abstract class BehaviourTree : MonoBehaviour
    {
        [field: SerializeField] public Node Root { get; set; }

        #region Properties

        [field: SerializeField] public EnemyBehaviourMachine BehaviourMachine { get; private set; }

        /// <summary>
        /// 当前行为树状态（用于调试显示）
        /// </summary>
        public NodeState CurrentState;

        /// <summary>
        /// 动作执行器实例（每个行为树独立拥有）
        /// </summary>
        public EnemyActions Actions { get; private set; }

        /// <summary>
        /// 条件检查器实例（每个行为树独立拥有）
        /// </summary>
        public EnemyConditions Conditions { get; private set; }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// 构建行为树，由子类实现
        /// </summary>
        public virtual void BuildTree()
        {
            Actions = new EnemyActions(this);
            Conditions = new EnemyConditions(this);
        }

        private void Awake()
        {
            BehaviourMachine = GetComponent<EnemyBehaviourMachine>();
        }

        private void Start()
        {
            BuildTree();
        }

        /// <summary>
        /// 每帧更新行为树状态
        /// </summary>
        private void Update()
        {
            if (Root != null)
            {
                CurrentState = Root.Evaluate();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 受到伤害，增加架势值
        /// </summary>
        /// <param name="damage">伤害值</param>
        public virtual void TakeDamage(float damage)
        {
        }

        #endregion
    }
}
