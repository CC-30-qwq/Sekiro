using Sekiro.BehaviourMachine;

namespace Sekiro.Enemy.Behaviours
{
    public class Enemy_ParryBehaviour : EnemyBaseBehaviour
    {
        public Enemy_ParryBehaviour(EnemyBehaviourMachine machine) : base(machine) { }

        public override bool CanChangeCondition()
        {
            return Machine.CurrentBehaviour != Machine.NextBehaviour;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            Methods.FadeAnimation("Idle");
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.UpdateRotation(Variable.TargetDirection, CharacterConfig.NormalRotateSpeed);
        }
    }
}
