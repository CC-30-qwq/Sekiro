using System.Collections.Generic;
using Sekiro.BehaviourMachine;
using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.Enemy.Setting
{
    /// <summary>
    /// 敌人攻击行为映射配置资产
    /// 将 EnemyBehaviour 枚举值映射到具体的 ComboConfigSO 连击配置、攻击权重和距离范围。
    /// 
    /// 合并自：
    ///   - AttackWeightConfig（权重/距离）
    ///   - EnemyConfigSO.AttackWeightEntry（权重/距离）
    ///   - 原有的 AttackEntry（连击映射）
    /// 三者合一后消除了 EnemyBehaviourMachine.UpdateAvailableAttacks() 中每帧的 struct 拷贝转换。
    /// </summary>
    [CreateAssetMenu(menuName = "Sekiro/Enemy/AttackMapping", fileName = "EnemyAttackMapping_")]
    public class EnemyAttackMappingSO : ScriptableObject
    {
        [System.Serializable]
        public struct AttackEntry
        {
            /// <summary>行为枚举值（如 Skill_0, Attack_0 等）</summary>
            public EnemyBehaviour behaviour;

            /// <summary>连击配置资产引用 — 替代旧的 ComboDatabase.GetCombo(int) 魔法数字</summary>
            public ComboConfigSO comboConfig;

            /// <summary>连击次数</summary>
            public int comboCount;

            /// <summary>攻击权重，权重越高被选中的概率越大</summary>
            [Range(0.1f, 10f)]
            public float weight;

            /// <summary>最小有效距离（米）</summary>
            public float minDistance;

            /// <summary>最大有效距离（米）</summary>
            public float maxDistance;
        }

        [SerializeField] private List<AttackEntry> _entries = new();

        /// <summary>
        /// 获取所有攻击映射条目（只读枚举，用于运行时距离过滤）
        /// </summary>
        public IReadOnlyList<AttackEntry> Entries => _entries;

        /// <summary>
        /// 根据行为枚举值查找对应的攻击映射条目
        /// </summary>
        public AttackEntry GetEntry(EnemyBehaviour behaviour)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].behaviour == behaviour)
                    return _entries[i];
            }

            Debug.LogError($"[EnemyAttackMappingSO] 未找到行为 {behaviour} 的映射配置");
            return new AttackEntry { behaviour = behaviour, weight = 0f, comboCount = 0 };
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器工具：验证所有攻击行为枚举是否都有对应条目
        /// </summary>
        public void ValidateMapping()
        {
            var defined = new HashSet<EnemyBehaviour>();
            foreach (var entry in _entries)
            {
                if (!defined.Add(entry.behaviour))
                    Debug.LogWarning($"[EnemyAttackMappingSO] 重复的行为映射: {entry.behaviour}");
            }
        }
#endif
    }
}
