using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class StartJumpInPlace : AirBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.StartJumpInPlace;

        public StartJumpInPlace(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Jump_Start");
            Variable.JumpDirection = Vector2.zero;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
        }
    }
}
