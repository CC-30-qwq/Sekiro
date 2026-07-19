using Sekiro.BehaviourMachine;
using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.Enemy.Behaviours
{
    /// <summary>
    /// 通用攻击行为 — 数据驱动 + 策略模式版本
    /// 通过 EnemyAttackMappingSO 自动从当前 EnemyBehaviour 枚举读取连击配置，
    /// 消除了 Enemy_Attack_0 / Enemy_Skill_0~5 等 9 个完全相同的样板类。
    /// 
    /// [重构] switch-case → 策略模式：
    /// - AttackType 对应的阶段逻辑（UpdateMeleeAttack/UpdateRangedAttack）
    ///   已提取到 IEnemyAttackStrategy 实现类中
    /// - OnUpdate 中 switch(_currentCombo.AttackType) 分支已消除
    /// - 新增攻击类型：新建策略实现 + 在工厂注册
    /// </summary>
    public class Enemy_AttackBehaviour : EnemyBaseBehaviour
    {
        public Enemy_AttackBehaviour(EnemyBehaviourMachine machine) : base(machine)
        {
        }

        #region Private Fields

        private ComboConfigSO _currentCombo;
        private bool _needsComboInit;
        private Vector3 _attackDirection;

        /// <summary>本行为对应的连击次数 — 从 AttackMapping 配置读取</summary>
        private int _comboCount;

        private bool _hasEnableCollider;

		/// <summary>远程攻击窗口索引 — 标记下一个待触发的远程窗口位置</summary>
		private int _nextRangedWindowIndex;

        /// <summary>攻击阶段驱动器（值类型，零GC）</summary>
        private AttackPhaseDriver _phaseDriver;

        /// <summary>当前机器上的 EnemyBehaviourMachine 快捷引用</summary>
        private EnemyBehaviourMachine EnemyMachine => Machine as EnemyBehaviourMachine;

        /// <summary>缓存上一段连击的 AttackData，供连招链结束时继续推进阶段到 Exit</summary>
        private AttackData _lastAttackData;

        /// <summary>暴露 Variable 给策略类（策略模式需要访问）</summary>
        internal new EnemyVariable Variable => base.Variable;

        /// <summary>暴露 Machine 给策略类</summary>
        internal new BehaviourMachine<EnemyBehaviour, EnemyMethods, EnemyVariable, EnemySetting> Machine => base.Machine;

        #endregion

        public override bool CanChangeCondition()
        {
            return _phaseDriver.CanChangeState && Machine.CurrentBehaviour != Machine.NextBehaviour;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _needsComboInit = true;
            _hasEnableCollider = false;
            _nextRangedWindowIndex = 0;

            // ---- 数据驱动：从 AttackMapping 配置自动读取映射 ----
            var mapping = EnemyMachine?.AttackMapping;
            if (mapping != null)
            {
                var entry = mapping.GetEntry(Machine.CurrentBehaviour);
                _comboCount = entry.comboCount;
                if (entry.comboConfig != null)
                    Variable.CurrentCombo = entry.comboConfig;
            }
            else
            {
                Debug.LogWarning($"[Enemy_AttackBehaviour] AttackMapping 未配置，行为 {Machine.CurrentBehaviour} 无法获取连击配置");
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // 使用 ComboConfigSO 直接引用获取当前连击配置
            _currentCombo = Variable.CurrentCombo;

            if (_currentCombo != null)
            {
                // ── 正常连击流程 ──
                if (_needsComboInit && _comboCount > 0)
                {
                    _comboCount--;
                    InitializeCurrentCombo();
                }
                else if (_needsComboInit)
                {
                    InitializeCurrentCombo();
                }

                // 推进阶段时序 — 敌人当前无需同步攻击状态到 ConditionEvaluator（敌人不依赖可视化配置），
                // 但保留变量绑定供未来调试/日志使用，消除 out _ 隐藏返回值的问题
                AttackWeaponHelper.AdvancePhase(ref _phaseDriver, _currentCombo, out var currentAttackState, out var isAttacking);

                // [解耦] 近战和远程攻击触发分离：
                // - TryEnableWeaponCollider — 管理近战碰撞体（Weapon）
                // - TryFireRangedAttack — 管理远程投射物（RangedWeapon）
                TryEnableWeaponCollider();

                // 远程攻击：计算指向目标的发射方向，委托给 AttackWeaponHelper 统一触发
                Vector3 targetDir = Variable.Target != null
                    ? (Variable.Target.position - Machine.transform.position).normalized
                    : Machine.transform.forward;
                AttackWeaponHelper.TryFireRangedAttack(
                    Machine.RangedWeapons,
                    _currentCombo,
                    ref _phaseDriver,
                    ref _nextRangedWindowIndex,
                    targetDir);

                // [策略模式] 根据攻击类型委托给策略实现
                var strategy = EnemyAttackStrategyFactory.GetStrategy(_currentCombo.AttackType);
                if (strategy != null)
                {
                    strategy.OnUpdate(this, ref _phaseDriver, _currentCombo, ref _attackDirection, ref _needsComboInit);
                }
            }
            else
            {
                // ── 连招链已结束（NextComboOnAttack == null）──
                // 使用上一段的 _lastAttackData 继续推进阶段，
                // 让 _phaseDriver 自然走到 Exit，解除 CanChangeCondition 阻塞
                _phaseDriver.Advance(_lastAttackData);
            }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.UpdateOriginRootPosition();
            Methods.UpdateRotation(_attackDirection, CharacterConfig.NormalRotateSpeed);
        }

        /// <summary>
        /// 初始化当前连击 — 统一处理动画淡入 + AttackPhaseDriver 初始化 + 碰撞器标志重置
        /// </summary>
        private void InitializeCurrentCombo()
        {
            _needsComboInit = false;
            _hasEnableCollider = false;
            _nextRangedWindowIndex = 0;

            _attackDirection = Variable.TargetDirection != Vector3.zero ? Variable.TargetDirection : Machine.transform.forward;

            // 缓存当前连击的时序数据，供连招链结束后继续推进阶段到 Exit
            _lastAttackData = _currentCombo.ToAttackData();

            // 使用 AttackWeaponHelper 统一处理动画淡入 + AttackPhaseDriver 初始化
            AttackWeaponHelper.InitializeCombo(
                Machine.ComboDatabase,
                (hash, time) => Methods.FadeAnimation(hash, time),
                _currentCombo,
                ref _phaseDriver);
        }

        /// <summary>
        /// 在攻击阶段中点启用武器碰撞器（仅执行一次）
        /// 委托给 AttackWeaponHelper 静态方法消除与 GroundAttackBehaviour 的重复代码
        /// </summary>
        protected void TryEnableWeaponCollider()
        {
            AttackWeaponHelper.TryEnableWeaponCollider(
                Machine.Weapons,
                _currentCombo,
                ref _phaseDriver,
                ref _hasEnableCollider);
        }

    }
}
