using System;
using System.Collections.Generic;
using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(WeaponManager))]
    [RequireComponent(typeof(RootMotionExtractor))]

    public abstract class BehaviourMachine<T, TMethods, TVariable, TSettings> : MonoBehaviour

            where T : struct, Enum
            where TMethods : BehaviourMethods<TSettings, TVariable>
            where TVariable : CharacterVariable
            where TSettings : CharacterSetting


    {
        public T LastBehaviour => lastBehaviourType;
        public T CurrentBehaviour => currentBehaviourType;
        public T NextBehaviour => nextBehaviourType;
        [field: SerializeField] protected T lastBehaviourType;
        [field: SerializeField] protected T currentBehaviourType;
        [field: SerializeField] protected T nextBehaviourType;

        [field: SerializeField] public Animator Animator { get; private set; }
        [field: SerializeField] public Rigidbody Rigidbody { get; private set; }
        [field: SerializeField] public Health Health { get; private set; }

        /// <summary>武器管理 — 委托给 WeaponManager 组件</summary>
        private WeaponManager _weaponManager;
        public Weapon[] Weapons => _weaponManager != null ? _weaponManager.Weapons : null;
        public RangedWeapon[] RangedWeapons => _weaponManager != null ? _weaponManager.RangedWeapons : null;

        /// <summary>根运动提取 — 委托给 RootMotionExtractor 组件</summary>
        private RootMotionExtractor _rootMotion;

        [field: SerializeField] public TMethods BehaviourMethods { get; private set; }
        [field: SerializeField] public TVariable Variable { get; private set; }
        [field: SerializeField] public TSettings Setting { get; private set; }

        /// <summary>
        /// 连击数据库 — 替代旧的 CharacterSetting.CombatUtility.ComboList
        /// 通过 SO 资产独立管理，支持跨角色复用
        /// </summary>
        [field: SerializeField] public ComboDatabaseSO ComboDatabase { get; private set; }

        /// <summary>
        /// 角色配置资产 — 集中管理移动/战斗/动画参数
        /// 替代旧的 PlayerStatus/EnemyStatus 序列化类
        /// </summary>
        [field: SerializeField] public CharacterConfigSO CharacterConfig { get; private set; }

        /// <summary>
        /// 动画配置资产 — 集中管理动画名称/Hash/淡入淡出时间
        /// 替代旧的 PlayerAnimHash/EnemyAnimHash 静态字典
        /// </summary>
        [field: SerializeField] public AnimationConfigSO AnimationConfig { get; private set; }

        /// <summary>
        /// 行为实例字典 - 延迟到 Start() 中初始化
        /// </summary>
        protected Dictionary<T, BaseBehaviour<T, TMethods, TVariable, TSettings>> behaviours;

        /// <summary>当前行为实例（受保护字段）</summary>
        protected BaseBehaviour<T, TMethods, TVariable, TSettings> currentBehaviour;

        /// <summary>
        /// 当前行为实例的公共只读属性
        /// 供外部组件（如可视化调试器）访问，消除反射依赖
        /// </summary>
        public BaseBehaviour<T, TMethods, TVariable, TSettings> CurrentBehaviourInstance => currentBehaviour;

        /// <summary>
        /// 初始化所有状态实例 — 子类在此注册 1:1 和 1:N 状态映射。
        /// </summary>
        protected abstract void InitializeStates();


        /// <summary>
        /// 验证所有枚举值是否已注册（调试模式断言，发布模式警告日志）
        /// 防止新增状态时忘记注册导致运行时静默切换失败
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        protected void ValidateStateRegistration()
        {
            var enumValues = System.Enum.GetValues(typeof(T));
            foreach (T value in enumValues)
            {
                if (!behaviours.ContainsKey(value))
                {
                    Debug.LogWarning($"[BehaviourMachine] 枚举值 {typeof(T).Name}.{value} 未注册任何行为实例！" +
                        $"请确保在 InitializeStates() 中注册，或确认该枚举值不需要对应行为。");
                }
            }
        }

        /// <summary>
        /// 激活初始状态（默认使用 Idle 状态作为起点）
        /// 提取共用样板代码，消除子类中重复的 TryGetValue + OnEnter 调用
        /// </summary>
        protected void ActivateInitialState(T initialState)
        {
            if (behaviours.TryGetValue(initialState, out var initialBehaviour))
            {
                currentBehaviour = initialBehaviour;
                currentBehaviour.OnEnter();
            }
            else
            {
                Debug.LogError($"[BehaviourMachine] 初始状态 {typeof(T).Name}.{initialState} 未注册！");
            }
        }

        protected virtual void InitializeSetting()
        {
            Variable.RootMovePosition = Vector3.zero;
            Variable.FaceDirection = transform.rotation;
        }

        protected void OnValidate()
        {
            if (!Application.isPlaying && Setting?.ColliderUtility != null)
            {
                // 在编辑器中初始化并重算碰撞器尺寸，确保 Inspector 参数调整时实时预览。
                // GetComponent 为只读操作，不会修改场景状态。
                Setting.ColliderUtility.Initialize(gameObject);
                Setting.ColliderUtility.CalculateCapsuleColliderDimensions();
            }
        }

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Animator = GetComponent<Animator>();
            Rigidbody = GetComponent<Rigidbody>();
            _weaponManager = GetComponent<WeaponManager>();
            _rootMotion = GetComponent<RootMotionExtractor>();

            Setting?.ColliderUtility?.Initialize(gameObject);
        }

        protected virtual void Start()
        {
            behaviours = new Dictionary<T, BaseBehaviour<T, TMethods, TVariable, TSettings>>();

            BehaviourMethods = CreateMethods();
            BehaviourMethods.Variable = Variable;
            BehaviourMethods.Setting = Setting;
            BehaviourMethods.Animator = Animator;
            BehaviourMethods.Rigidbody = Rigidbody;
            BehaviourMethods.Transform = transform;
            BehaviourMethods.AnimConfig = AnimationConfig;
            BehaviourMethods.CharacterConfig = CharacterConfig;

            // 构建动画Hash缓存（避免每帧调用 ToLegacyHashDict 产生GC分配）
            BehaviourMethods.BuildAnimHashCache();

            // 连线 RootMotionExtractor
            if (_rootMotion != null)
            {
                _rootMotion.Animator = Animator;
                _rootMotion.Variable = Variable;
                _rootMotion.Config = CharacterConfig;
            }

            InitializeSetting();
            InitializeStates();
        }

        /// <summary>
        /// 创建具体的方法类实例
        /// </summary>
        protected abstract TMethods CreateMethods();

        protected virtual void Update()
        {
            currentBehaviour?.OnUpdate();
            currentBehaviour?.EvaluateStateTransitionRules();

            // [安全] 双重判空防止 MissingReferenceException：
            // Unity 对象在销毁后 != null 为 true 但仍会抛异常（假 null）
            var target = Variable.Target;
            if (target != null && target)
            {
                Variable.TargetDirection = target.position - transform.position;
                Variable.CachedDistance = Vector3.Distance(transform.position, target.position);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (currentBehaviour?.ShouldApplyFloat == true)
                BehaviourMethods.Float();
            currentBehaviour?.OnFixedUpdate();
        }

        /// <summary>
        /// 根运动更新 - 从动画提取位移并应用速度缩放
        /// 直接从 CharacterConfigSO 读取 ModifyMoveSpeed
        /// [优化] 将 CharacterConfig 空检查提取为局部变量，消除重复判断
        /// </summary>
        protected virtual void OnAnimatorMove()
        {
            _rootMotion?.ApplyRootMotion();
        }

        /// <summary>
        /// 安全切换：使用异常保护防止状态机进入不一致状态。
        /// 支持完全回滚：当 OnExit() 成功但 OnEnter() 失败时，
        /// 会退出新状态并重新进入旧状态以恢复其内部状态。
        /// </summary>
        public virtual void TryChangeState(T newState)
        {
            nextBehaviourType = newState;

            // 获取目标状态实例
            if (behaviours == null || !behaviours.TryGetValue(newState, out var newBehaviour))
            {
                nextBehaviourType = currentBehaviourType;
                return;
            }

            // 检查当前状态是否允许切换
            if (currentBehaviour != null && !currentBehaviour.CanChangeCondition())
            {
                nextBehaviourType = currentBehaviourType;
                return;
            }

            // 安全切换：使用异常保护防止状态机进入不一致状态
            T previousType = currentBehaviourType;
            BaseBehaviour<T, TMethods, TVariable, TSettings> previousBehaviour = currentBehaviour;

            try
            {
                lastBehaviourType = currentBehaviourType;
                currentBehaviour?.OnExit();
                currentBehaviour = newBehaviour;
                currentBehaviourType = newState;
                nextBehaviourType = newState;

                currentBehaviour.OnEnter();
            }
            catch (System.Exception ex)
            {
                // 发生异常时回滚到上一个稳定状态
                // 注意：previousBehaviour.OnExit() 已调用，若 newBehaviour.OnEnter() 失败，
                // 需要先退出 newBehaviour，再重新进入 previousBehaviour 恢复其内部状态
                Debug.LogError($"[BehaviourMachine] 状态切换异常: {previousType} -> {newState}, 错误: {ex.Message}");

                // 如果 currentBehaviour 已被设置为 newBehaviour，先退出它
                if (currentBehaviour == newBehaviour && newBehaviour != previousBehaviour)
                {
                    try { newBehaviour.OnExit(); }
                    catch { /* 静默忽略清理中的二次异常 */ }
                }

                // 完全回滚：恢复指针和枚举字段
                currentBehaviour = previousBehaviour;
                currentBehaviourType = previousType;
                nextBehaviourType = previousType;

                // 重新进入上一个状态（之前已调用 OnExit，需恢复其内部状态机）
                // 例如 Timer 计时器、协程、订阅的事件等
                previousBehaviour?.OnEnter();
            }
        }

        protected virtual void OnDestroy()
        {
            try
            {
                currentBehaviour?.OnExit();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BehaviourMachine] OnExit 异常: {ex.Message}");
            }
            currentBehaviour = null;

            if (behaviours != null)
            {
                behaviours.Clear();
                behaviours = null;
            }

            BehaviourMethods = null;
        }
    }
}
