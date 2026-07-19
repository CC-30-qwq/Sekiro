using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class StartJumpDirect : AirBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.StartJumpDirect;

        public StartJumpDirect(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Jump_Start_Free");
            Variable.HorizontalSpeed = CharacterConfig.JumpForce;
            Variable.JumpDirection = Vector2.up;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Persist;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.AddHorizontalForce(Variable.HorizontalSpeed, Variable.BufferedDirection, true);
        }
    }
}
