using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 远程攻击策略 — 对应 AttackType.RangedAttack
    /// 
    /// 行为逻辑：
    /// - Attacking 阶段：锁定攻击方向为角色面朝方向
    /// - Recovery/Exit 阶段：检测输入尝试进入下一段连击
    /// 
    /// [解耦] 远程攻击的投射物生成已独立到 RangedWeapon，
    /// 由 GroundAttackBehaviour.TryFireRangedAttack() 统一管理，
    /// 不再依赖 Weapon.HitboxWindowRoutine。
    /// </summary>
    public class RangedAttackStrategy : IComboAttackStrategy
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
                case AttackState.Attacking:
                    // 攻击中锁定方向
                    attackDirection = behaviour.Machine.transform.forward;
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
