using System.Collections.Generic;
using UnityEngine;

namespace Sekiro.Character.Data
{
    /// <summary>
    /// 动画配置资产 — 集中管理动画名称、Hash 与淡入淡出时间
    /// 替代旧的 PlayerAnimHash/EnemyAnimHash 静态字典，
    /// 支持通过 SO 资产独立配置不同角色的动画参数
    /// </summary>
    [CreateAssetMenu(menuName = "Sekiro/Animation/Config", fileName = "AnimConfig_")]
    public class AnimationConfigSO : ScriptableObject
    {
        [System.Serializable]
        public class AnimEntry
        {
            [Tooltip("动画状态机中的状态名称")]
            public string Name;

            [Tooltip("淡入淡出过渡时间（秒）")]
            public float CrossFadeTime = 0.1f;

            /// <summary>运行时计算的动画Hash值</summary>
            [HideInInspector] public int Hash;
        }

        [SerializeField]
        private List<AnimEntry> _entries = new();

        /// <summary>名称→Hash 查找表（运行时构建）</summary>
        private Dictionary<string, int> _hashCache;

        /// <summary>名称→CrossFadeTime 查找表（运行时构建）</summary>
        private Dictionary<string, float> _fadeTimeCache;

        /// <summary>Hash→CrossFadeTime 缓存（替代 ToLegacyHashDict 每帧创建新字典）</summary>
        private Dictionary<int, float> _legacyHashCache;

        private void OnEnable()
        {
            BuildCache();
        }

        /// <summary>
        /// 构建运行时缓存（在 Start/OnEnable 中调用）
        /// </summary>
        public void BuildCache()
        {
            _hashCache = new Dictionary<string, int>(_entries.Count);
            _fadeTimeCache = new Dictionary<string, float>(_entries.Count);
            _legacyHashCache = new Dictionary<int, float>(_entries.Count);

            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

                entry.Hash = Animator.StringToHash(entry.Name);
                _hashCache[entry.Name] = entry.Hash;
                _fadeTimeCache[entry.Name] = entry.CrossFadeTime;
                _legacyHashCache[entry.Hash] = entry.CrossFadeTime;
            }
        }

        /// <summary>
        /// 获取动画状态Hash
        /// </summary>
        public int GetHash(string name)
        {
            if (_hashCache != null && _hashCache.TryGetValue(name, out int hash))
                return hash;

            // 容错：未缓存时实时计算
            return Animator.StringToHash(name);
        }

        /// <summary>
        /// 获取动画状态的淡入淡出时间
        /// </summary>
        public float GetFadeTime(string name)
        {
            if (_fadeTimeCache != null && _fadeTimeCache.TryGetValue(name, out float time))
                return time;

            return 0.1f; // 默认值
        }

        /// <summary>
        /// 转换为旧的 Dictionary[int, float] 格式（向后兼容）
        /// 使用内置预计算缓存，零GC分配
        /// </summary>
        public Dictionary<int, float> ToLegacyHashDict()
        {
            // 返回预计算缓存的引用，避免每帧创建新字典
            if (_legacyHashCache != null)
                return _legacyHashCache;

            // 容错：缓存未构建时回退到实时计算
            _legacyHashCache = new Dictionary<int, float>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                int hash = Animator.StringToHash(entry.Name);
                _legacyHashCache[hash] = entry.CrossFadeTime;
            }
            return _legacyHashCache;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下获取所有条目（用于 Inspector 显示）
        /// </summary>
        public IReadOnlyList<AnimEntry> Entries => _entries;

        /// <summary>
        /// 编辑器下批量添加条目（用于资产生成工具）
        /// </summary>
        public void Editor_AddEntries(IEnumerable<KeyValuePair<string, float>> namedEntries)
        {
            _entries.Clear();
            foreach (var kvp in namedEntries)
            {
                _entries.Add(new AnimEntry
                {
                    Name = kvp.Key,
                    CrossFadeTime = kvp.Value
                });
            }
        }
#endif
    }
}
