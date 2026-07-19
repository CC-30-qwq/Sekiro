using System;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class Hit : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.Hit;

        public Hit(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Vector2 direction = Methods.GetHitDirection(LastDamageInfo.Direction, Machine.transform);

            if (direction == Vector2.up)
                Methods.FadeAnimation("Hit_Light_F");
            else if (direction == Vector2.down)
                Methods.FadeAnimation("Hit_Light_B");
            else if (direction == Vector2.left)
                Methods.FadeAnimation("Hit_Light_L");
            else if (direction == Vector2.right)
                Methods.FadeAnimation("Hit_Light_R");
            else
                Methods.FadeAnimation("Hit_Light_F");

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
