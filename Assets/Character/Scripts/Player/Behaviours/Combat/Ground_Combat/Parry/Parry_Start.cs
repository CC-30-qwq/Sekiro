using System;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class ParryStart : ParryBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.ParryStart;

        public ParryStart(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Parry_Start");
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
        }
    }
}
