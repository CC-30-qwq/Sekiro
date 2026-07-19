using Sekiro.BehaviourMachine;
using Sekiro.Character.Data;
using Sekiro.Combat;
using Sekiro.Enemy.Setting;
using UnityEngine.AI;

namespace Sekiro.Enemy.Behaviours
{
    public class EnemyBaseBehaviour : BaseBehaviour<EnemyBehaviour, EnemyMethods, EnemyVariable, EnemySetting>
    {
        protected NavMeshAgent Agent => agent;
        protected readonly NavMeshAgent agent;

        /// <summary>
        /// 角色通用配置辅助属性 — 从 Machine.CharacterConfig 直接读取
        /// 替代旧的 Setting.CharacterStatus 配置字段（移动参数等）
        /// </summary>
        protected CharacterConfigSO CharacterConfig => Machine.CharacterConfig;

        /// <summary>
        /// 敌人特有配置辅助属性 — 从 EnemyBehaviourMachine.EnemyConfig 直接读取
        /// 替代旧的 Setting.CharacterStatus 配置字段（攻击范围、权重等）
        /// </summary>
        protected EnemyConfigSO EnemyConfig => (Machine as EnemyBehaviourMachine)?.EnemyConfig;

        protected internal override bool ShouldApplyFloat => true;

        public EnemyBaseBehaviour(EnemyBehaviourMachine machine) : base(machine)
        {
            this.agent = machine.GetComponent<NavMeshAgent>();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
        }
    }
}
