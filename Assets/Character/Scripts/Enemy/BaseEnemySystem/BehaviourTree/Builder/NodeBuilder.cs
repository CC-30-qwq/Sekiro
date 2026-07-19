using System;
using System.Collections.Generic;

namespace Sekiro.BehaviourTree.Builder
{
    /// <summary>
    /// 行为树流畅构建器 — 消除 new List<Node> 样板代码
    ///
    /// 注意：使用全限定名避免与 Sekiro.BehaviourTree 命名空间的 Condition / Action 类名冲突。
    /// 通过lambda包裹解决 Func<> 与自定义委托类型不可隐式转换的问题。
    /// </summary>
    public static class NodeBuilder
    {
        /// <summary>序列节点：所有子节点依次执行，任一失败则整体失败</summary>
        public static SequenceBuilder Sequence() => new SequenceBuilder();

        /// <summary>选择节点：按顺序尝试子节点，任一成功则整体成功</summary>
        public static SelectorBuilder Selector() => new SelectorBuilder();

        /// <summary>加权随机选择节点：按权重随机选择子节点执行</summary>
        public static WeightedRandomBuilder WeightedRandom() => new WeightedRandomBuilder();

        /// <summary>条件节点：条件满足返回 Success，否则返回 Failure</summary>
        public static Node Condition(Func<bool> conditionPredicate)
            => new Sekiro.BehaviourTree.Condition(() => conditionPredicate());

        /// <summary>动作节点：执行指定动作并返回状态</summary>
        public static Node Action(Func<NodeState> actionDelegate)
            => new Sekiro.BehaviourTree.Action(() => actionDelegate());
    }

    /// <summary>
    /// Sequence 构建器
    /// </summary>
    public class SequenceBuilder
    {
        private readonly List<Node> _children = new();

        public SequenceBuilder Then(Node child)
        {
            _children.Add(child);
            return this;
        }

        public Node Build() => new Sequence(_children);
    }

    /// <summary>
    /// Selector 构建器
    /// </summary>
    public class SelectorBuilder
    {
        private readonly List<Node> _children = new();

        public SelectorBuilder Try(Node child)
        {
            _children.Add(child);
            return this;
        }

        public Node Build() => new Selector(_children);
    }

    /// <summary>
    /// WeightedRandomSelector 构建器
    /// </summary>
    public class WeightedRandomBuilder
    {
        private readonly List<Node> _children = new();
        private readonly List<float> _weights = new();

        public WeightedRandomBuilder Option(Node child, float weight)
        {
            _children.Add(child);
            _weights.Add(weight);
            return this;
        }

        public Node Build() => new WeightedRandomSelector(_children, _weights);
    }
}
