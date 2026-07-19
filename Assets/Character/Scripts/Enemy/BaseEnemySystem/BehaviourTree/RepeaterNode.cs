namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 重复节点：将子节点重复执行指定次数（-1 表示无限循环）
    /// </summary>
    public class Repeater : Node
    {
        private readonly Node _child;
        private readonly int _maxCount;
        private int _currentCount;

        public Repeater(Node child, int maxCount = -1)
        {
            _child = child;
            _maxCount = maxCount;
            _currentCount = 0;
        }

        public override NodeState Evaluate()
        {
            if (_maxCount > 0 && _currentCount >= _maxCount)
            {
                State = NodeState.Success;
                return State;
            }

            var result = _child.Evaluate();

            if (result == NodeState.Success || result == NodeState.Failure)
            {
                _currentCount++;
            }

            State = NodeState.Running;
            return State;
        }

        /// <summary>
        /// 重置重复计数（用于重新开始循环）
        /// </summary>
        public void Reset()
        {
            _currentCount = 0;
        }
    }
}
