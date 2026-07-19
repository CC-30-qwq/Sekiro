using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    public class TurnStart : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.TurnStart;

        public TurnStart(PlayerBehaviourMachine machine) : base(machine)
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
            Methods.FadeAnimation(StateData.TurnIsLeft ? "Turn_Start_0" : "Turn_Start_1");
        }
    }
}
