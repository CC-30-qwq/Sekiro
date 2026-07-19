using System.Collections.Generic;

namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 并行节点：同时执行所有子节点
    ///   - needAllSuccess = true → 等待所有子节点成功才返回 Success，任一失败则返回 Failure
    ///   - needAllSuccess = false → 任一子节点成功就返回 Success，全部失败才返回 Failure
    /// </summary>
    public class Parallel : Node
    {
        private readonly List<Node> _children;
        private readonly bool _needAllSuccess;

        public Parallel(List<Node> children, bool needAllSuccess = true)
        {
            _children = children;
            _needAllSuccess = needAllSuccess;
        }

        public override NodeState Evaluate()
        {
            bool hasRunning = false;

            foreach (var child in _children)
            {
                var result = child.Evaluate();

                if (_needAllSuccess)
                {
                    if (result == NodeState.Failure)
                    {
                        State = NodeState.Failure;
                        return State;
                    }
                    if (result == NodeState.Running)
                        hasRunning = true;
                }
                else
                {
                    if (result == NodeState.Success)
                    {
                        State = NodeState.Success;
                        return State;
                    }
                    if (result == NodeState.Running)
                        hasRunning = true;
                }
            }

            State = hasRunning ? NodeState.Running
                : _needAllSuccess ? NodeState.Success : NodeState.Failure;
            return State;
        }
    }
}
