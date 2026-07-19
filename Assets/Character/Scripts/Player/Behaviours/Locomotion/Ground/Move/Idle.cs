using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    public class Idle : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.Idle;

        public Idle(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Profile = PlayerBehaviourProfile.GroundDefault(CharacterConfig.FallForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Persist;
        }
    }
}
