using System;
using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 地面攻击行为 — 使用策略模式管理不同攻击类型的阶段逻辑
    /// 
    /// [重构] switch-case → 策略模式：
    /// - AttackType 对应的阶段逻辑已提取到 IComboAttackStrategy 实现类中
    /// - 新增攻击类型：新建策略实现 + 在工厂注册，无需修改此类
    /// - OnUpdate 中 switch(_currentCombo.AttackType) + Update*() 方法已消除
    /// </summary>
    public class AttackGround : GroundAttackBehaviour
    {
        internal override PlayerBehaviour? StateBehaviour => PlayerBehaviour.AttackGround;

        private bool _hasRelease;

        public AttackGround(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Profile.EnableHitTransition = true;
            Locomotion.BufferInput = LocomotionDriver.BufferInputMode.Forward;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // 使用 ComboConfigSO 直接引用获取当前连击配置
            _currentCombo = Variable.CurrentCombo;
            if (_currentCombo == null) return;

            if (_needsComboInit)
            {
                _hasRelease = false;
                InitializeCombo();
            }

            if (VisualIsAttacking)
            {
                AdvancePhase();
                // 每帧尝试启用 HitCollider — 由 _hasEnableCollider + TotalElapsedTime 决定实际触发时机
                // 不依赖具体阶段，EnableTime 可以从 Initialize 起跨越 Charging/Windup
                TryEnableWeaponCollider();

                // [解耦] 远程攻击投射物触发 — 独立于近战碰撞体管理
                TryFireRangedAttack();
            }

            // [策略模式] 根据攻击类型委托给策略实现
            var strategy = ComboAttackStrategyFactory.GetStrategy(_currentCombo.AttackType);
            if (strategy != null)
            {
                strategy.OnUpdate(
                    this,
                    _currentCombo,
                    ref _attackDirection,
                    ref _needsComboInit,
                    ref _hasRelease);
            }
        }

    }
}
