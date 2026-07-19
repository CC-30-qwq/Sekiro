using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class LandInPlace : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.LandInPlace;

        public LandInPlace(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Land");
            Profile.ShouldApplyFloat = true;
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Persist;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.UpdateVerticalPosition(Vector3.up);
        }
    }
}
