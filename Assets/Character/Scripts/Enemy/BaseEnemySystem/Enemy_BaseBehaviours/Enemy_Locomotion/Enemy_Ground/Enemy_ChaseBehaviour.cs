using Sekiro.BehaviourMachine;

namespace Sekiro.Enemy.Behaviours
{
    public class Enemy_ChaseBehaviour : Enemy_GroundBehaviour
    {
        public Enemy_ChaseBehaviour(EnemyBehaviourMachine machine) : base(machine) { }

        public override void OnEnter()
        {
            base.OnEnter();

            Methods.FadeAnimation("Run");
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.FindPath(Variable.Target);
            Methods.UpdateModifyRootPosition();
            Methods.UpdateRotation(Variable.PathDirection, CharacterConfig.SprintRotateSpeed);
        }
    }
}
