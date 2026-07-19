using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.Enemy
{
    /// <summary>
    /// 敌人可检测适配器 — 将敌人实体适配到 IDetectable 接口
    /// 
    /// 设计思路：
    /// - 挂载在敌人 GameObject 根节点上
    /// - 自动查找 CharacterTarget 子物体作为检测点（HitPoint）
    /// - 通过 Health 组件判断是否存活
    /// - 继承自 MonoBehaviour 方便挂载和序列化
    /// - 完全解耦 PlayerVision 与 enemy.transform.Find("CharacterTarget") 的硬编码依赖
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public class EnemyDetectable : MonoBehaviour, IDetectable
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private string _characterTargetPath = "CharacterTarget";

        [Header("Detection Settings")]
        [SerializeField] private float _priorityBoost = 0f;

        #endregion

        #region Private Fields

        private Transform _characterTarget;
        private Health _health;

        #endregion

        #region IDetectable Implementation

        public Transform RootTransform => transform;

        public Transform DetectPoint
        {
            get
            {
                // 智能回退：有 CharacterTarget 子物体则使用它，否则回退到根物体
                EnsureCharacterTargetCached();
                return _characterTarget ?? transform;
            }
        }

        public bool IsDetectable
        {
            get
            {
                EnsureHealthCached();
                return _health != null && _health.IsAlive;
            }
        }

        public float PriorityBoost => _priorityBoost;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        #endregion

        #region Private Methods

        private void CacheComponents()
        {
            _health = GetComponent<Health>();
            CacheCharacterTarget();
        }

        private void EnsureHealthCached()
        {
            if (_health == null)
                _health = GetComponent<Health>();
        }

        private void EnsureCharacterTargetCached()
        {
            if (_characterTarget == null)
                CacheCharacterTarget();
        }

        private void CacheCharacterTarget()
        {
            var found = transform.Find(_characterTargetPath);
            if (found != null)
            {
                _characterTarget = found;
            }
        }

        #endregion
    }
}
