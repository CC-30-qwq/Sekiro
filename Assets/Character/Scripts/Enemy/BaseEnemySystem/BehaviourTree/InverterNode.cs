namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 反相节点：反转子节点的执行结果
    /// </summary>
    public class Inverter : Node
    {
        private readonly Node _child;

        public Inverter(Node child)
        {
            _child = child;
        }

        public override NodeState Evaluate()
        {
            switch (_child.Evaluate())
            {
                case NodeState.Failure:
                    State = NodeState.Success;
                    break;
                case NodeState.Success:
                    State = NodeState.Failure;
                    break;
                case NodeState.Running:
                    State = NodeState.Running;
                    break;
            }

            return State;
        }
    }
}
