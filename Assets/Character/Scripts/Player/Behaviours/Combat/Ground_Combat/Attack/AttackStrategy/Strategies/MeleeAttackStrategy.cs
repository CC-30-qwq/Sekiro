using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 近战连击策略 — 对应 AttackType.MeleeAttack
    /// 
    /// 行为逻辑：
    /// - Windup 阶段：保持攻击方向指向输入方向
    /// - Recovery/Exit 阶段：检测输入尝试进入下一段连击
    /// （无 Charging/Attacking 阶段特殊逻辑）
    /// </summary>
    public class MeleeAttackStrategy : IComboAttackStrategy
    {
        public void OnUpdate(
            GroundAttackBehaviour behaviour,
            ComboConfigSO currentCombo,
            ref Vector3 attackDirection,
            ref bool needsComboInit,
            ref bool hasRelease)
        {
            var state = behaviour.PhaseDriver.CurrentState;
            switch (state)
            {
                case AttackState.Windup:
                    // 前摇时攻击方向跟随输入
                    attackDirection = behaviour.Variable.InputDirection != Vector3.zero
                        ? behaviour.Variable.InputDirection
                        : attackDirection;
                    break;

                case AttackState.Recovery:
                case AttackState.Exit:
                    // 收招阶段尝试下一段连击
                    AttackWeaponHelper.TryNextCombo(
                        behaviour.Inputs, InputType.Attack,
                        currentCombo.NextComboOnAttack,
                        behaviour.Variable, ref needsComboInit);
                    break;
            }
        }
    }
}
