using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 冲刺攻击策略 — 对应 AttackType.SprintAttack
    /// 
    /// 行为逻辑：
    /// - Charging 阶段：检测 chargeInput 松手→标记 hasRelease（在 Windup 时释放）
    /// - Windup 阶段：保持攻击方向指向输入方向，如果 Charging 已松手则切连击
    /// - Attacking 阶段：锁定攻击方向为角色面朝方向
    /// - Recovery/Exit 阶段：检测输入尝试进入下一段连击
    /// </summary>
    public class SprintAttackStrategy : IComboAttackStrategy
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
                case AttackState.Charging:
                    // 前摇时攻击方向跟随输入
                    attackDirection = behaviour.Variable.InputDirection != Vector3.zero
                        ? behaviour.Variable.InputDirection
                        : attackDirection;

                    // Charging 阶段检测松手
                    if (!behaviour.Inputs.ChargeInput)
                    {
                        hasRelease = true;
                    }
                    break;

                case AttackState.Attacking:
                    // 攻击中锁定方向
                    attackDirection = behaviour.Machine.transform.forward;

                    // 如果 Charging 时已松手，立即切连击
                    if (hasRelease)
                    {
                        behaviour.Variable.CurrentCombo = currentCombo.NextComboOnRelease;
                        needsComboInit = true;
                        hasRelease = false;
                    }
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
