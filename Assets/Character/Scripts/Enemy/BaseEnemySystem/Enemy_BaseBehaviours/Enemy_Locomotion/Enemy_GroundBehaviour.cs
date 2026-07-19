using Sekiro.BehaviourMachine;

namespace Sekiro.Enemy.Behaviours
{
    public class Enemy_GroundBehaviour : EnemyBaseBehaviour
    {
        public Enemy_GroundBehaviour(EnemyBehaviourMachine machine) : base(machine)
        {
        }

        public override bool CanChangeCondition()
        {
            return Machine.CurrentBehaviour != Machine.NextBehaviour;
        }
    }
}
