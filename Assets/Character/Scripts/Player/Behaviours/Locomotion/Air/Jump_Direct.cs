using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class JumpDirect : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.JumpDirect;

        private bool _ready;

        public JumpDirect(PlayerBehaviourMachine machine) : base(machine) { }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Jump_Free");
            _ready = true;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
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
            Methods.AddHorizontalForce(Variable.HorizontalSpeed, Variable.BufferedDirection, true);
            Methods.AddHorizontalForce(CharacterConfig.AirMoveSpeed, Variable.InputDirection, false);
        }
    }
}
