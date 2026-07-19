using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class Sprint : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.Sprint;

        private Vector3 _sprintDirection;

        public Sprint(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _sprintDirection = Machine.transform.forward;
            Methods.FadeAnimation("Move_Hard");
            Profile = PlayerBehaviourProfile.GroundDefault(CharacterConfig.JumpForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.FixedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Forward;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            _sprintDirection = RootMotionUtility.SmoothRotateDirection(_sprintDirection, Variable.InputDirection, CharacterConfig.SprintRotateSpeed);
            StateData.SprintReleaseTimerValue = Inputs.SprintInput ? 0f : StateData.SprintReleaseTimerValue + Time.deltaTime;
        }

        public override void OnFixedUpdate()
        {
            Locomotion.FixedDirection = _sprintDirection;
            base.OnFixedUpdate();
        }
    }
}
