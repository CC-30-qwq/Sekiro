using System.Collections.Generic;

namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 选择节点：按顺序尝试执行子节点，任一成功则返回成功
    /// </summary>
    public class Selector : Node
    {
        private readonly List<Node> _children;

        public Selector(List<Node> children)
        {
            _children = children;
        }

        public override NodeState Evaluate()
        {
            foreach (Node node in _children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Failure:
                        continue;
                    case NodeState.Success:
                        State = NodeState.Success;
                        return State;
                    case NodeState.Running:
                        State = NodeState.Running;
                        return State;
                    default:
                        continue;
                }
            }

            State = NodeState.Failure;
            return State;
        }
    }
}
