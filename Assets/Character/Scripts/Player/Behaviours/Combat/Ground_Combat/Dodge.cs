using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class Dodge : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.Dodge;

        private Vector3 _dodgeDirection;

        public Dodge(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            GuardUtility.ApplyGuardResistance(Machine.Health, 0.4f);
            Methods.FadeAnimation("Dodge_F");
            Profile = PlayerBehaviourProfile.CombatDefault(CharacterConfig.JumpForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Direct;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.FixedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Forward;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            _dodgeDirection = RootMotionUtility.SmoothRotateDirection(_dodgeDirection, Variable.InputDirection, CharacterConfig.SprintRotateSpeed);

            if (Timer < 0.1f)
            {
                _dodgeDirection = Variable.InputDirection.sqrMagnitude > Constants.VectorNearZeroThreshold ? Variable.InputDirection : Machine.transform.forward;
            }

            StateData.SprintHoldTimerValue = !Inputs.SprintInput ? 0f : StateData.SprintHoldTimerValue + Time.deltaTime;
        }

        public override void OnFixedUpdate()
        {
            Locomotion.FixedDirection = _dodgeDirection;
            base.OnFixedUpdate();
        }
    }
}
