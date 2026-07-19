namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 条件节点：执行条件检查并返回结果
    /// </summary>
    public class Condition : Leaf
    {
        public delegate bool ConditionDelegate();

        private ConditionDelegate _condition;

        public Condition(ConditionDelegate condition)
        {
            _condition = condition;
        }

        public override NodeState Evaluate()
        {
            return _condition() ? NodeState.Success : NodeState.Failure;
        }
    }
}
