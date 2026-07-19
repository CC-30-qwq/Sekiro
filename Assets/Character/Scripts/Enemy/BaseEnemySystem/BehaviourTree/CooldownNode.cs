using UnityEngine;

namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 冷却节点：子节点执行成功后进入冷却，冷却期间返回 Failure
    /// </summary>
    public class Cooldown : Node
    {
        private readonly Node _child;
        private readonly float _cooldownTime;
        private float _lastSuccessTime;

        public Cooldown(Node child, float cooldownTime)
        {
            _child = child;
            _cooldownTime = cooldownTime;
            _lastSuccessTime = -cooldownTime; // 首次执行无需等待冷却
        }

        public override NodeState Evaluate()
        {
            if (Time.time - _lastSuccessTime < _cooldownTime)
            {
                State = NodeState.Failure;
                return State;
            }

            var result = _child.Evaluate();

            if (result == NodeState.Success)
            {
                _lastSuccessTime = Time.time;
            }

            State = result;
            return State;
        }
    }
}
