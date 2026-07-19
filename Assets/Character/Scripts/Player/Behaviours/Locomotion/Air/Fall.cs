using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class Fall : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.Fall;

        public Fall(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Fall");
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.InputDirection;
            Locomotion.RotationSpeed = CharacterConfig.AirRotateSpeed;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Methods.AddGravityForce(true);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.UpdateVerticalPosition(Vector3.up);
            Methods.AddHorizontalForce(Variable.HorizontalSpeed, Variable.BufferedDirection, true);
            Methods.AddHorizontalForce(CharacterConfig.AirMoveSpeed, Variable.InputDirection, false);
        }
    }
}
