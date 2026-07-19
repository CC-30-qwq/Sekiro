using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    public class TurnMedium : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.TurnMedium;

        public TurnMedium(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            StateData.TurnIsLeft = Variable.AnimInputValue.x < 0;
            StateData.TurnIsRight = Variable.AnimInputValue.x >= 0;
            Profile = PlayerBehaviourProfile.GroundDefault(CharacterConfig.FallForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Origin;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Persist;
            Methods.FadeAnimation(StateData.TurnIsLeft ? "Turn_Medium_0" : "Turn_Medium_1");
        }
    }
}
