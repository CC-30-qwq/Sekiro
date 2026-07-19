namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 直到失败节点：重复执行子节点，直到子节点返回 Failure
    /// </summary>
    public class UntilFailure : Node
    {
        private readonly Node _child;

        public UntilFailure(Node child)
        {
            _child = child;
        }

        public override NodeState Evaluate()
        {
            var result = _child.Evaluate();

            if (result == NodeState.Failure)
            {
                State = NodeState.Failure;
                return State;
            }

            State = NodeState.Running;
            return State;
        }
    }
}
