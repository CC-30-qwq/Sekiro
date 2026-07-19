using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class LandDirect : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.LandDirect;

        public LandDirect(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Land_Free");
            Profile.ShouldApplyFloat = true;
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Forward;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.UpdateVerticalPosition(Vector3.up);
        }
    }
}
