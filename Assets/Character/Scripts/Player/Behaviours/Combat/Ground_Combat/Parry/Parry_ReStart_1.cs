using System;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class ParryReStart1 : ParryBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.ParryReStart1;

        public ParryReStart1(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Parry_ReStart_1");
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
        }
    }
}
