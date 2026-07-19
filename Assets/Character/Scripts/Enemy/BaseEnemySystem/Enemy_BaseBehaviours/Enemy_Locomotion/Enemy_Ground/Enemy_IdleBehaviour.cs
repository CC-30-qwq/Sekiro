using Sekiro.BehaviourMachine;

namespace Sekiro.Enemy.Behaviours
{
    public class Enemy_IdleBehaviour : Enemy_GroundBehaviour
    {
        public Enemy_IdleBehaviour(EnemyBehaviourMachine machine) : base(machine) { }

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
