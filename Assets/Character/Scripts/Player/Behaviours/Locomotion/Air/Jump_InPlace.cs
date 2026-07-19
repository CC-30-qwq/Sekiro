using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class JumpInPlace : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.JumpInPlace;

        private bool _ready;

        public JumpInPlace(PlayerBehaviourMachine machine) : base(machine) { }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Jump");
            _ready = true;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.InputDirection;
            Locomotion.RotationSpeed = CharacterConfig.AirRotateSpeed;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (_ready)
            {
                if (Timer > 0.1f)
                {
                    _ready = false;
                    Methods.AddVerticalForce(CharacterConfig.JumpHeight * CharacterConfig.GravityMultiplier);
                }
            }
            else
            {
                Methods.AddGravityForce(true);
            }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.UpdateVerticalPosition(Vector3.up);
            Methods.AddHorizontalForce(CharacterConfig.AirMoveSpeed, Variable.InputDirection, false);
        }
    }
}
