using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class StartLand : AirBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.StartLand;

        public StartLand(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Land_Start");
            Methods.AddGravityForce(false);
            Profile.ShouldApplyFloat = true;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Forward;
        }
    }
}
