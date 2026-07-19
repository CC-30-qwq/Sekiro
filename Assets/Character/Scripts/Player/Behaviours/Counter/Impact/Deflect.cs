using System;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class Deflect : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.Deflect;

        public Deflect(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Vector2 direction = Methods.GetHitDirection(LastDamageInfo.Direction, Machine.transform);

            if (direction == Vector2.up)
                Methods.FadeAnimation("Deflect_Light_C");
            else if (direction == Vector2.left)
                Methods.FadeAnimation("Deflect_Light_L");
            else if (direction == Vector2.right)
                Methods.FadeAnimation("Deflect_Light_R");
            else
                Methods.FadeAnimation("Deflect_Light_C");

            Profile.ShouldApplyFloat = true;
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Forward;
        }

        protected override void BuildStateTransitionRules(System.Collections.Generic.List<(Func<bool>, Action)> rules)
        {
            base.BuildStateTransitionRules(rules);
            rules.Add((() => IsHit, () => Machine.TryChangeState(PlayerBehaviour.Hit)));
        }
    }
}
