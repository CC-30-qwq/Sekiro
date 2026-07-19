using System;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class Block : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.Block;

        public Block(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            GuardUtility.ApplyGuardResistance(Machine.Health, 0.4f);

            Vector2 direction = Methods.GetHitDirection(LastDamageInfo.Direction, Machine.transform);

            if (direction == Vector2.up)
                Methods.FadeAnimation("Block_Light_C0");
            else if (direction == Vector2.left)
                Methods.FadeAnimation("Block_Light_L0");
            else if (direction == Vector2.right)
                Methods.FadeAnimation("Block_Light_R0");
            else
                Methods.FadeAnimation("Block_Light_C0");

            Profile = PlayerBehaviourProfile.CombatDefault(CharacterConfig.FallForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
        }

        protected override void BuildStateTransitionRules(System.Collections.Generic.List<(Func<bool>, Action)> rules)
        {
            base.BuildStateTransitionRules(rules);
            GuardUtility.BuildGuardTransitionRules(rules, this);
        }
    }
}
