using System.Collections.Generic;
using UnityEngine;

namespace Sekiro.Character.Data
{
    /// <summary>
    /// 连击数据库 — 一个角色的所有连击配置
    /// 
    /// 【索引兼容说明】
    /// 同时保留 GetCombo(index) 索引查找和 GetIndex(combo) 引用反查，
    /// 以便在状态转换可视化编辑器中仍然可以使用 SetComboIndex 动作。
    /// 新代码优先使用 ComboConfigSO 直接引用（NextComboOnAttack 等），
    /// 不再依赖列表顺序。
    /// </summary>
    [CreateAssetMenu(menuName = "Sekiro/Combat/Combo Database", fileName = "ComboDatabase_")]
    public class ComboDatabaseSO : ScriptableObject
    {
        [SerializeField]
        private List<ComboConfigSO> _combos = new();

        /// <summary>
        /// 动画Hash 实例缓存（生命周期与 SO 实例绑定）
        /// 使用 Lazy 初始化，首次访问时构建
        /// </summary>
        private Dictionary<string, int> _animHashCache;

        /// <summary>
        /// 连击总数
        /// </summary>
        public int Count => _combos.Count;

        /// <summary>
        /// 安全获取连击配置（按索引，带越界检查）
        /// </summary>
        public ComboConfigSO GetCombo(int index)
        {
            if (index >= 0 && index < _combos.Count)
                return _combos[index];

            Debug.LogError($"[ComboDatabase] 连击索引越界: {index}, 总数: {_combos.Count}");
            return null;
        }

        /// <summary>
        /// 尝试获取连击配置（无 GC 分配的 out 模式）
        /// </summary>
        public bool TryGetCombo(int index, out ComboConfigSO combo)
        {
            if (index >= 0 && index < _combos.Count)
            {
                combo = _combos[index];
                return true;
            }

            combo = null;
            return false;
        }

        /// <summary>
        /// 获取连击对应的索引（用于兼容旧的状态转换编辑器）
        /// 如果列表中不包含该连击，返回 -1
        /// </summary>
        public int GetIndex(ComboConfigSO combo)
        {
            if (combo == null) return -1;
            return _combos.IndexOf(combo);
        }

        /// <summary>
        /// 判断连击是否在数据库中
        /// </summary>
        public bool Contains(ComboConfigSO combo)
        {
            return combo != null && _combos.Contains(combo);
        }

        /// <summary>
        /// 获取动画名称对应的 Animator Hash（实例缓存，生命周期与 SO 绑定）
        /// 直接内联调用，不再经过已删除的 AnimHashCacheUtility 中介
        /// </summary>
        public int GetAnimHash(string animationClipName)
        {
            if (string.IsNullOrEmpty(animationClipName))
                return 0;

            // 延迟初始化缓存
            if (_animHashCache == null)
            {
                _animHashCache = new Dictionary<string, int>(_combos.Count);
            }

            if (_animHashCache.TryGetValue(animationClipName, out int hash))
                return hash;

            // 未命中时实时计算并缓存
            hash = Animator.StringToHash(animationClipName);
            _animHashCache[animationClipName] = hash;
            return hash;
        }

        /// <summary>
        /// 当 SO 实例被 Unity 卸载时自动清理实例缓存
        /// </summary>
        private void OnDisable()
        {
            _animHashCache = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下批量获取所有动画剪辑名称
        /// </summary>
        public IEnumerable<string> GetAllAnimationClipNames()
        {
            foreach (var combo in _combos)
            {
                if (combo != null && !string.IsNullOrEmpty(combo.AnimationClipName))
                    yield return combo.AnimationClipName;
            }
        }

        /// <summary>
        /// 编辑器下批量设置连击列表（用于资产生成工具）
        /// </summary>
        public void Editor_SetCombos(IEnumerable<ComboConfigSO> combos)
        {
            _combos.Clear();
            if (combos != null)
            {
                _combos.AddRange(combos);
            }
            // 清空Hash缓存，下次访问时重建
            _animHashCache = null;
        }
#endif
    }
}

