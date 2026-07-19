using System;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class ParryReStart0 : ParryBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.ParryReStart0;

        public ParryReStart0(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Parry_ReStart_0");
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
        }
    }
}
