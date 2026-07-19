using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    public class StartRun : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.StartRun;

        public StartRun(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Start_Medium");
            Profile = PlayerBehaviourProfile.GroundDefault(CharacterConfig.FallForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Persist;
        }
    }
}
