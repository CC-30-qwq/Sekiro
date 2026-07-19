using System.Collections.Generic;
using Sekiro.BehaviourTree.Utils;

namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 加权随机选择节点：根据权重随机选择子节点执行
    /// 内部使用 WeightedRandomUtil 消除算法重复
    /// </summary>
    public class WeightedRandomSelector : Node
    {
        private readonly List<Node> _children;
        private readonly List<float> _weights;

        public WeightedRandomSelector(List<Node> children, List<float> weights)
        {
            _children = children;
            _weights = weights;
            if (children.Count != weights.Count)
            {
                throw new System.ArgumentException("Children and weights lists must have the same length");
            }
        }

        public override NodeState Evaluate()
        {
            int selectedIndex = WeightedRandomUtil.SelectIndex(_weights);
            if (selectedIndex < 0 || selectedIndex >= _children.Count)
            {
                State = NodeState.Failure;
                return State;
            }

            return _children[selectedIndex].Evaluate();
        }
    }
}
