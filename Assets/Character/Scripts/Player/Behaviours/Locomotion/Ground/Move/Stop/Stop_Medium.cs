using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    public class StopMedium : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.StopRun;

        public StopMedium(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Stop_Medium");
            Profile = PlayerBehaviourProfile.GroundDefault(CharacterConfig.FallForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Persist;
        }
    }
}
