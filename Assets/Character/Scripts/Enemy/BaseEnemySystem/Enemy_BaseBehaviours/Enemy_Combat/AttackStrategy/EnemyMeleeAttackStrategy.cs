using Sekiro.BehaviourMachine;
using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.Enemy.Behaviours
{
    /// <summary>
    /// 敌人近战攻击策略 — 对应 AttackType.MeleeAttack
    /// 
    /// 行为逻辑：
    /// - Windup 阶段：设置攻击方向指向目标
    /// - Recovery 阶段：切换到下一段连击
    /// </summary>
    public class EnemyMeleeAttackStrategy : IEnemyAttackStrategy
    {
        public void OnUpdate(
            Enemy_AttackBehaviour behaviour,
            ref AttackPhaseDriver phaseDriver,
            ComboConfigSO currentCombo,
            ref Vector3 attackDirection,
            ref bool needsComboInit)
        {
            var state = phaseDriver.CurrentState;
            if (state == AttackState.Windup)
            {
                attackDirection = behaviour.Variable.TargetDirection != Vector3.zero
                    ? behaviour.Variable.TargetDirection
                    : behaviour.Machine.transform.forward;
            }
            else if (state == AttackState.Recovery)
            {
                behaviour.Variable.CurrentCombo = currentCombo.NextComboOnAttack;
                needsComboInit = true;
            }
        }
    }
}
