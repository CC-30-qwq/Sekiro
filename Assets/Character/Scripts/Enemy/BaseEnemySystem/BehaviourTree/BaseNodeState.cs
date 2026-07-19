namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 节点状态枚举
    /// </summary>
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }

    /// <summary>
    /// 行为树节点基类
    /// </summary>
    public abstract class Node
    {
        private NodeState _state;

        protected Node()
        {
            _state = NodeState.Running;
        }

        public abstract NodeState Evaluate();

        protected NodeState State
        {
            get => _state;
            set => _state = value;
        }
    }

    /// <summary>
    /// 叶子节点基类
    /// </summary>
    public abstract class Leaf : Node
    {
    }
}
