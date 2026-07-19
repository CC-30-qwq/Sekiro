using System.Collections.Generic;
using Sekiro.BehaviourTree.Utils;
using Sekiro.Enemy.Behaviours;
using Sekiro.Enemy.Setting;
using UnityEngine;
using UnityEngine.AI;

namespace Sekiro.BehaviourMachine
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyBehaviourMachine : BehaviourMachine<EnemyBehaviour, EnemyMethods, EnemyVariable, EnemySetting>
    {
        [field: SerializeField] public NavMeshAgent Agent { get; private set; }

        /// <summary>
        /// 敌人配置资产 — 特有攻击范围配置
        /// </summary>
        [field: SerializeField] public EnemyConfigSO EnemyConfig { get; private set; }

        /// <summary>
        /// 攻击行为映射配置 — 将 EnemyBehaviour 枚举映射到 ComboConfigSO，
        /// 同时包含权重和距离范围配置（合并自 AttackWeightConfig / AttackWeightEntry）
        /// </summary>
        [field: SerializeField] public EnemyAttackMappingSO AttackMapping { get; private set; }

        /// <summary>
        /// 从 EnemyConfigSO 获取距离范围判定
        /// 委托给 DistanceUtility 统一实现，消除与 EnemyConditions 的代码重复
        /// </summary>
        public DistanceRange GetDistanceRange()
        {
            return DistanceUtility.GetDistanceRange(Variable.CachedDistance, EnemyConfig);
        }

        #region Performance Optimization

        private const float _groundCheckInterval = Constants.GroundCheckInterval; // 50Hz
        private const float _distanceChangeThreshold = 0.1f; // 距离变化阈值，低于此值不重新计算可用攻击列表
        private float _lastGroundCheckTime;
        private float _lastDistanceForAttacks; // 上次计算可用攻击列表时的距离值

        /// <summary>
        /// 可复用攻击列表缓存 - 避免每帧new List产生GC
        /// 直接复用 EnemyAttackMappingSO.AttackEntry，消除旧的 struct 拷贝转换
        /// </summary>
        private readonly List<EnemyAttackMappingSO.AttackEntry> _availableAttacksCache = new List<EnemyAttackMappingSO.AttackEntry>();

        #endregion

        protected override EnemyMethods CreateMethods()
        {
            return new EnemyMethods();
        }

        protected override void InitializeStates()
        {
            behaviours.Add(EnemyBehaviour.Idle, new Enemy_IdleBehaviour(this));
            behaviours.Add(EnemyBehaviour.Stander, new Enemy_StanderBehaviour(this));
            behaviours.Add(EnemyBehaviour.Chase, new Enemy_ChaseBehaviour(this));

            // 数据驱动：所有攻击/技能行为统一使用 Enemy_AttackBehaviour
            // comboCount / CurrentCombo 由 EnemyAttackMappingSO 配置自动注入
            behaviours.Add(EnemyBehaviour.Attack0, new Enemy_AttackBehaviour(this));
            behaviours.Add(EnemyBehaviour.Dodge, new Enemy_AttackBehaviour(this));
            behaviours.Add(EnemyBehaviour.Skill0, new Enemy_AttackBehaviour(this));
            behaviours.Add(EnemyBehaviour.Skill1, new Enemy_AttackBehaviour(this));
            behaviours.Add(EnemyBehaviour.Skill2, new Enemy_AttackBehaviour(this));
            behaviours.Add(EnemyBehaviour.Skill3, new Enemy_AttackBehaviour(this));
            behaviours.Add(EnemyBehaviour.Skill4, new Enemy_AttackBehaviour(this));
            behaviours.Add(EnemyBehaviour.Skill5, new Enemy_AttackBehaviour(this));
            behaviours.Add(EnemyBehaviour.SkillCounter, new Enemy_AttackBehaviour(this));
            behaviours.Add(EnemyBehaviour.SkillExtension, new Enemy_AttackBehaviour(this));

            ActivateInitialState(EnemyBehaviour.Idle);
        }

        protected override void InitializeSetting()
        {
            base.InitializeSetting();
            Agent.updatePosition = false;
            Agent.updateRotation = false;
        }

        protected override void Awake()
        {
            base.Awake();
            Agent = GetComponent<NavMeshAgent>();
        }

        protected override void Start()
        {
            base.Start();
            BehaviourMethods.Agent = Agent;
        }

        protected override void Update()
        {
            base.Update();

            Agent.nextPosition = transform.position;
            // 使用.Length而非Count()避免LINQ开销
            if (Agent.path.corners != null && Agent.path.corners.Length > 1)
            {
                Variable.PathDirection = Agent.path.corners[1] - Agent.path.corners[0];
            }

            UpdateAvailableAttacks();
        }

        /// <summary>
        /// 更新可用攻击列表 - 从 EnemyAttackMappingSO.Entries 直接读取权重/距离配置
        /// 消除了旧的 AttackWeightConfig struct 拷贝转换（attackState/weight/minDistance/maxDistance）
        /// 仅当距离变化超过阈值时才重新计算，避免每帧GC分配
        /// </summary>
        private void UpdateAvailableAttacks()
        {
            var distance = Variable.CachedDistance;

            // 距离变化未超过阈值时跳过，减少每帧List重新构建
            if (Mathf.Abs(distance - _lastDistanceForAttacks) < _distanceChangeThreshold)
                return;

            _lastDistanceForAttacks = distance;
            _availableAttacksCache.Clear();

            if (AttackMapping == null)
            {
                Variable.AvailableAttacks = _availableAttacksCache;
                return;
            }

            foreach (var entry in AttackMapping.Entries)
            {
                if (distance >= entry.minDistance && distance <= entry.maxDistance)
                {
                    _availableAttacksCache.Add(entry);
                }
            }

            Variable.AvailableAttacks = _availableAttacksCache;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            float currentTime = Time.time;
            if (currentTime - _lastGroundCheckTime >= _groundCheckInterval)
            {
                BehaviourMethods.GroundCheck();
                _lastGroundCheckTime = currentTime;
            }
        }

        // OnAnimatorMove 继承基类默认实现（根运动位移提取）
    }
}
