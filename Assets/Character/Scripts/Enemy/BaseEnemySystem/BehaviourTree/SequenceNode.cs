using System.Collections.Generic;

namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 序列节点：按顺序执行所有子节点，任一失败则返回失败
    /// </summary>
    public class Sequence : Node
    {
        private readonly List<Node> _children;

        public Sequence(List<Node> children)
        {
            _children = children;
        }

        public override NodeState Evaluate()
        {
            bool anyChildRunning = false;

            foreach (Node node in _children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Failure:
                        State = NodeState.Failure;
                        return State;
                    case NodeState.Success:
                        continue;
                    case NodeState.Running:
                        anyChildRunning = true;
                        continue;
                    default:
                        State = NodeState.Success;
                        return State;
                }
            }

            State = anyChildRunning ? NodeState.Running : NodeState.Success;
            return State;
        }
    }
}
