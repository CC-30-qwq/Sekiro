using System;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class ParrySprint : ParryBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.ParrySprint;

        public ParrySprint(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Parry_Sprint");
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.SprintRotateSpeed;
        }
    }
}
