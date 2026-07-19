using System;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// Parry behaviour base class - 招架行为基类。
    /// 使用 ParryPhaseDriver 值类型管理三段时序（Windup → Parrying → Recovery）。
    /// </summary>
    public class ParryBehaviour : PlayerBaseBehaviour
    {
        protected internal ParryPhaseDriver ParryDriver;

        public ParryState VisualCurrentPhase => ParryDriver.CurrentPhase;
        public float VisualPhaseProgress => ParryDriver.PhaseProgress;
        public float VisualTotalProgress => ParryDriver.TotalProgress;
        public bool IsParryComplete => ParryDriver.IsComplete;

        public ParryBehaviour(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            ParryDriver.Initialize(0.1f, 0.1f, 0.1f);
            Profile = PlayerBehaviourProfile.CombatDefault(CharacterConfig.FallForce);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            ParryDriver.Advance();

            StateData.CurrentParryStateValue = ParryDriver.CurrentPhase;
            StateData.IsParryingValue = !ParryDriver.IsComplete;

            if (Machine.Health != null)
            {
                if (ParryDriver.CurrentPhase == ParryState.Parrying || ParryDriver.CurrentPhase == ParryState.Windup)
                {
                    Machine.Health.DamageResistanceModifier = 1f;
                }
                else
                {
                    Machine.Health.DamageResistanceModifier = 0.4f;
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            if (Machine.Health != null)
                Machine.Health.DamageResistanceModifier = 0f;
            ParryDriver.Reset();
            StateData.IsParryingValue = false;
        }

        protected override void BuildStateTransitionRules(System.Collections.Generic.List<(Func<bool>, Action)> rules)
        {
            base.BuildStateTransitionRules(rules);
            rules.Add((() => IsHit && Methods.GetHitDirection(LastDamageInfo.Direction, Machine.transform) != Vector2.down && ParryDriver.CurrentPhase == ParryState.Windup, () => Machine.TryChangeState(PlayerBehaviour.Deflect)));
            rules.Add((() => IsHit && Methods.GetHitDirection(LastDamageInfo.Direction, Machine.transform) != Vector2.down && ParryDriver.CurrentPhase == ParryState.Parrying, () => Machine.TryChangeState(PlayerBehaviour.Deflect)));
            rules.Add((() => IsHit && Methods.GetHitDirection(LastDamageInfo.Direction, Machine.transform) != Vector2.down && ParryDriver.CurrentPhase == ParryState.Recovery, () => Machine.TryChangeState(PlayerBehaviour.Block)));
            rules.Add((() => IsHit && Methods.GetHitDirection(LastDamageInfo.Direction, Machine.transform) == Vector2.down, () => Machine.TryChangeState(PlayerBehaviour.Hit)));
        }
    }
}
