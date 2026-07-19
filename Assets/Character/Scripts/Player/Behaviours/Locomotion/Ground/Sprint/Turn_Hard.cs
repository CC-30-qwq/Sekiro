using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    public class TurnHard : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.TurnHard;

        public TurnHard(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation(Variable.AnimInputValue.x < 0 ? "Turn_Hard_0" : "Turn_Hard_1");
            Profile = PlayerBehaviourProfile.GroundDefault(CharacterConfig.FallForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Origin;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.FixedDirection;
            Locomotion.FixedDirection = Variable.InputDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Forward;
        }
    }
}
