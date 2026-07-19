using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    public class StopHard : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.StopSprint;

        public StopHard(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Stop_Hard");
            Profile = PlayerBehaviourProfile.GroundDefault(CharacterConfig.FallForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Persist;
        }
    }
}
