namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 动作节点：执行指定动作并返回结果
    /// </summary>
    public class Action : Leaf
    {
        public delegate NodeState ActionDelegate();

        private ActionDelegate _action;

        public Action(ActionDelegate action)
        {
            _action = action;
        }

        public override NodeState Evaluate()
        {
            return _action();
        }
    }
}
