namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 直到成功节点：重复执行子节点，直到子节点返回 Success
    /// </summary>
    public class UntilSuccess : Node
    {
        private readonly Node _child;

        public UntilSuccess(Node child)
        {
            _child = child;
        }

        public override NodeState Evaluate()
        {
            var result = _child.Evaluate();

            if (result == NodeState.Success)
            {
                State = NodeState.Success;
                return State;
            }

            State = NodeState.Running;
            return State;
        }
    }
}
