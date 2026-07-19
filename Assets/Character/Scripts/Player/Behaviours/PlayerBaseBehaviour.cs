using System;
using Sekiro.Combat;
using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    public class PlayerBaseBehaviour : BaseBehaviour<PlayerBehaviour, PlayerMethods, PlayerVariable, PlayerSetting>, IConditionDataProvider

    {
        // === 类型收窄内部访问器（供 ConditionEvaluator / 策略类使用）===
        // 使用 new 关键字将基类泛型类型收窄为具体类型，避免下游强制转换。
        internal new PlayerBehaviourMachine Machine => (PlayerBehaviourMachine)base.Machine;
        internal new PlayerVariable Variable => base.Variable;
        internal new PlayerMethods Methods => base.Methods;
        /// <summary>
        /// 角色配置辅助属性 — 从 Machine.CharacterConfig 直接读取
        /// </summary>
        protected CharacterConfigSO CharacterConfig => Machine.CharacterConfig;

        /// <summary>
        /// 枚举标识 - 每个具体（叶子）状态类应重写此属性，返回对应的枚举值。
        /// 中间基类（TurnBehaviour/StopBehaviour 等）保持默认值 null，
        /// 它们不会被用作可视化配置的状态源。
        /// </summary>
        internal virtual PlayerBehaviour? StateBehaviour => null;

        /// <summary>
        /// 输入工具类访问器
        /// 优先使用 Methods.Inputs（Start 后已设置），早期生命周期回退到构造函数缓存的引用
        /// 两个引用指向同一个 InputsUtility 实例
        /// </summary>
        internal InputsUtility Inputs => Methods?.Inputs ?? _inputsCached;
        private readonly InputsUtility _inputsCached;

        // ======================================================================
        // 运行时状态数据（值类型，零GC）
        // 使用 BehaviourStateData 聚合所有 ConditionEvaluator 所需字段，
        // 减少类字段膨胀，通过 .Reset() 统一重置，消除遗漏风险
        // ======================================================================

        internal BehaviourStateData StateData;

        // ======================================================================
        // IConditionDataProvider explicit implementation
        // ======================================================================

        #region IConditionDataProvider Implementation

        /// <summary>非破坏性输入检测 — 委托给 Inputs</summary>
        bool IConditionDataProvider.PeekIns(InputType input) => Inputs.PeekIns(input);

        /// <summary>消耗输入缓冲区 — 委托给 Inputs</summary>
        void IConditionDataProvider.ConsumeInputBuffer() => Inputs.ConsumeInputBuffer();

        bool IConditionDataProvider.DefenceInput => Inputs.DefenceInput;
        bool IConditionDataProvider.SprintInput => Inputs.SprintInput;


        float IConditionDataProvider.Timer => base.Timer;

        bool IConditionDataProvider.IsGrounded => Variable.IsGrounded;
        Vector2 IConditionDataProvider.TargetVector => Variable.TargetVector;
        Vector2 IConditionDataProvider.AnimInputValue => Variable.AnimInputValue;
        float IConditionDataProvider.VerticalSpeed => Variable.VerticalSpeed;
        Vector2 IConditionDataProvider.JumpDirection => Variable.JumpDirection;

        ComboConfigSO IConditionDataProvider.CurrentCombo
        {
            get => Variable.CurrentCombo;
            set => Variable.CurrentCombo = value;
        }

        float IConditionDataProvider.HorizontalSpeed
        {
            get => Variable.HorizontalSpeed;
            set => Variable.HorizontalSpeed = value;
        }

        float IConditionDataProvider.SprintReleaseTimerValue => StateData.SprintReleaseTimerValue;
        float IConditionDataProvider.SprintHoldTimerValue => StateData.SprintHoldTimerValue;
        bool IConditionDataProvider.IsAttackingValue => StateData.IsAttackingValue;
        AttackState IConditionDataProvider.CurrentAttackStateValue => StateData.CurrentAttackStateValue;
        ParryState IConditionDataProvider.CurrentParryStateValue => StateData.CurrentParryStateValue;
        bool IConditionDataProvider.IsParryingValue => StateData.IsParryingValue;
        bool IConditionDataProvider.TurnIsLeft => StateData.TurnIsLeft;
        bool IConditionDataProvider.TurnIsRight => StateData.TurnIsRight;

        #endregion


        // ======================================================================

        /// <summary>
        /// 最近一次受击的完整 DamageInfo — 由 OnDamagedHandler 设置
        /// </summary>
        internal DamageInfo LastDamageInfo { get; private set; }

        /// <summary>
        /// 切换到 Fall 状态 — 消除 GroundBehaviour/CombatBehaviour/Dodge 中的重复实现
        /// </summary>
        /// <param name="horizontalForce">水平力值（各子类提供不同值）</param>
        protected void TransitionToFall(float horizontalForce)
        {
            Machine.Variable.HorizontalSpeed = horizontalForce;
            Machine.TryChangeState(PlayerBehaviour.Fall);
        }

        /// <summary>
        /// 可视化配置组件缓存
        /// </summary>
        private VisualConfigBehaviour _visualConfig;

        /// <summary>
        /// 缓存的可视化规则委托数组（避免每帧重新创建）
        /// </summary>
        private (Func<bool> Condition, Action Action)[] _cachedVisualRules;

        /// <summary>
        /// 记录缓存时的 VisualConfigBehaviour Version，用于检测配置是否变化
        /// </summary>
        private int _cachedVisualRulesVersion = -1;

        /// <summary>
        /// 移动驱动 — 统一调度根运动、旋转与输入缓冲，替代各子类的透视方法调用。
        /// 子类在 OnEnter 中设置配置，基类 OnFixedUpdate/OnUpdate 统一调用。
        /// </summary>
        protected readonly LocomotionDriver Locomotion = new LocomotionDriver();

        /// <summary>
        /// 状态配置 — 替代 GroundBehaviour/CombatBehaviour/SprintBehaviour 等中间件。
        /// 叶子状态在 OnEnter 中设置，PlayerBaseBehaviour 统一消费。
        /// </summary>
        protected PlayerBehaviourProfile Profile;

        /// <summary>
        /// 是否需要在 FixedUpdate 中执行 Float，由 Profile.ShouldApplyFloat 驱动。
        /// 替代各中间件的 virtual override。
        /// </summary>
        protected internal override bool ShouldApplyFloat => Profile.ShouldApplyFloat;

        public PlayerBaseBehaviour(PlayerBehaviourMachine machine) : base(machine)
        {
            _inputsCached = machine.GetComponent<InputsUtility>();
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _inputsCached?.Buffer?.ClearBuffer();

            // [M3] 重置状态数据至默认值，消除子类手动逐个重置的遗漏风险
            StateData.Reset();
            Locomotion.Reset();

            // [S1] 统一管理受击事件订阅
            if (Machine.Health != null)
                Machine.Health.OnDamaged += OnDamagedHandler;
        }

        public override void OnExit()
        {
            base.OnExit();
            // [S1] 统一取消受击事件订阅
            if (Machine.Health != null)
                Machine.Health.OnDamaged -= OnDamagedHandler;
            GuardUtility.ClearGuardResistance(Machine.Health);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Methods.CalculateTargetVector2();
            Methods.UpdateLocomotionParameters();
            Locomotion.ApplyUpdate(Methods, Variable, Machine.transform);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Locomotion.ApplyFixedUpdate(Rigidbody, Machine.transform, Variable, CharacterConfig, Methods);
        }

        /// <summary>
        /// 受击事件处理 — 替代原 DamageHitMonitor.OnDamaged
        /// 更新 Variable 中的受击标志位和方向，触发击退效果
        /// </summary>
        public void OnDamagedHandler(DamageInfo damageInfo)
        {
            if (Variable == null) return;

            LastDamageInfo = damageInfo;
            base.HandleDamage(damageInfo);

            CombatUtility.ApplyKnockback(Rigidbody, damageInfo, CharacterConfig);
        }

        // 状态转换评估：先执行代码内规则，再执行可视化配置规则
        public override void EvaluateStateTransitionRules()
        {
            foreach (var rule in StateTransitionRules)
            {
                if (rule.Condition.Invoke())
                {
                    rule.Action.Invoke();
                    return; // 状态可能已变化，立即停止评估避免连锁反应
                }
            }

            var visualRules = TryGetVisualRules();
            if (visualRules != null)
            {
                foreach (var rule in visualRules)
                {
                    if (rule.Condition.Invoke())
                    {
                        rule.Action.Invoke();
                        return; // 状态可能已变化，立即停止评估避免连锁反应
                    }
                }
            }
        }

        // #region Damage Event Management — 已融合至 BehaviourMachine 统一管理

        #region Visual Config Caching

        /// <summary>
        /// 尝试获取可视化配置的转换规则（带缓存，避免每帧重新创建委托数组）
        /// </summary>
        private (Func<bool> Condition, Action Action)[] TryGetVisualRules()
        {
            if (_visualConfig == null)
            {
                var machine = Machine as PlayerBehaviourMachine;
                if (machine != null)
                {
                    _visualConfig = machine.GetComponent<VisualConfigBehaviour>();
                }
            }

            if (_visualConfig == null)
                return null;

            // 检查配置状态（未设置时清除缓存并返回 null）
            if (_visualConfig.Config == null)
            {
                _cachedVisualRules = null;
                _cachedVisualRulesVersion = -1;
                return null;
            }

            // 通过 Version 检测配置是否变化
            var currentVersion = _visualConfig.Version;
            if (_cachedVisualRules != null && _cachedVisualRulesVersion == currentVersion)
            {
                return _cachedVisualRules;
            }

            // 缓存失效（首次或配置变化），重新获取规则并缓存
            _cachedVisualRules = _visualConfig.GetRulesForState(this);
            _cachedVisualRulesVersion = currentVersion;
            return _cachedVisualRules;
        }

        #endregion

        #region State Transition Rules (Inheritance-Aware)

        /// <summary>
        /// 缓存的转换规则数组 — 由 BuildStateTransitionRules 在首次访问时构建
        /// </summary>
        private (Func<bool> Condition, Action Action)[] _builtRules;

        /// <summary>
        /// 状态转换规则表（只读，继承链感知）
        /// 通过 BuildStateTransitionRules 虚方法构建，子类调用 base 追加规则而非替换
        /// 
        /// [重构] 从属性重写改为方法追加：
        /// - GroundBehaviour 定义受击/坠落检测 → 所有地面子类自动继承
        /// - 子类只需添加自身特有规则，不再担心覆盖基类规则
        /// - 零GC：构建一次后缓存
        /// </summary>
        protected (Func<bool> Condition, Action Action)[] StateTransitionRules
        {
            get
            {
                if (_builtRules == null)
                {
                    var list = new System.Collections.Generic.List<(Func<bool>, Action)>(8);
                    BuildStateTransitionRules(list);
                    _builtRules = list.ToArray();
                }
                return _builtRules;
            }
        }

        /// <summary>
        /// 构建状态转换规则列表（子类通过 base.BuildStateTransitionRules() 追加自身规则）
        /// 规则按添加顺序排列，越早添加的规则优先级越高
        /// </summary>
        /// <param name="rules">规则列表，基类先添加，子类后追加</param>
        protected virtual void BuildStateTransitionRules(System.Collections.Generic.List<(Func<bool>, Action)> rules)
        {
            // 基于 Profile 自动添加中间件级规则（替代 GroundBehaviour/CombatBehaviour）
            if (Profile.EnableHitTransition)
                rules.Add((() => IsHit, () => Machine.TryChangeState(PlayerBehaviour.Hit)));
            if (Profile.EnableFallTransition)
                rules.Add((() => !Variable.IsGrounded, () => TransitionToFall(Profile.FallForceValue)));
        }

        #endregion
    }
}
