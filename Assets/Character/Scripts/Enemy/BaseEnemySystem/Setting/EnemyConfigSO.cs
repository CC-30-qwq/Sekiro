using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.Enemy.Setting
{
    /// <summary>
    /// 敌人配置资产 — 敌人特有的攻击范围配置
    /// 权重和距离配置已合并到 EnemyAttackMappingSO.AttackEntry 中
    /// </summary>
    [CreateAssetMenu(menuName = "Sekiro/Enemy/Config", fileName = "EnemyConfig_")]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("基础配置（复用 CharacterConfigSO）")]
        public CharacterConfigSO BaseConfig;

        [Header("距离判定")]
        public float CloseRange = 5f;
        public float MidRange = 10f;
        public float FarRange = 20f;
    }
}
