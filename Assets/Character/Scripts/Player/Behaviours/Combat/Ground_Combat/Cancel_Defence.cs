using System;
using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    public class CancelDefence : PlayerBaseBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.CancelDefence;

        public CancelDefence(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Defence_End");
            Profile = PlayerBehaviourProfile.CombatDefault(CharacterConfig.FallForce);
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Forward;
        }

        protected override void BuildStateTransitionRules(System.Collections.Generic.List<(Func<bool>, Action)> rules)
        {
            base.BuildStateTransitionRules(rules);
            rules.Add((() => IsHit, () => Machine.TryChangeState(PlayerBehaviour.Hit)));
        }
    }
}
