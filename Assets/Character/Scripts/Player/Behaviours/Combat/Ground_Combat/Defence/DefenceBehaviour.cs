using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class DefenceBehaviour : PlayerBaseBehaviour
    {
        private readonly PlayerBehaviour _stateBehaviour;

        private static readonly Dictionary<PlayerBehaviour, string> AnimationNameMap = new()
        {
            { PlayerBehaviour.Defence, "Defence" },
            { PlayerBehaviour.DefenceMove, "Defence_Move" },
            { PlayerBehaviour.DefenceMoveStop, "Defence_Move_Stop" },
            { PlayerBehaviour.CancelDefence, "Cancel_Defence" },
        };

        internal override PlayerBehaviour? StateBehaviour => _stateBehaviour;

        public DefenceBehaviour(PlayerBehaviourMachine machine, PlayerBehaviour stateBehaviour) : base(machine)
        {
            _stateBehaviour = stateBehaviour;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            GuardUtility.ApplyGuardResistance(Machine.Health, 0.4f);

            string animName = AnimationNameMap.TryGetValue(_stateBehaviour, out var name)
                ? name
                : _stateBehaviour.ToString();
            Methods.FadeAnimation(animName);

            Profile = PlayerBehaviourProfile.CombatDefault(CharacterConfig.FallForce);
            Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
            Locomotion.ActiveRotation = LocomotionDriver.RotationSource.BufferedDirection;
            Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Persist;
        }

        protected override void BuildStateTransitionRules(System.Collections.Generic.List<(Func<bool>, Action)> rules)
        {
            base.BuildStateTransitionRules(rules);
            GuardUtility.BuildGuardTransitionRules(rules, this);
        }
    }
}
