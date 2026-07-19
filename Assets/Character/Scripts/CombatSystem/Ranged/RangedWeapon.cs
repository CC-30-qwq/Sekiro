using System.Collections.Generic;
using Sekiro.Character.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace Sekiro.Combat
{
    /// <summary>
    /// 远程武器组件 — 独立管理投射物的生成、对象池和生命周期
    /// 
    /// 职责分离：
    /// - Weapon（近战）：管理碰撞体、近战伤害检测
    /// - RangedWeapon（远程）：管理对象池、投射物生成、发射方向
    /// 
    /// 使用方式：
    /// 1. 在角色 Prefab 上挂载此组件（与 Weapon 组件相互独立）
    /// 2. 在 RangedHitWindow 中指定 WeaponIndex 映射到此组件
    /// 3. WeaponIndex 索引基于 Weapon[] 和此组件在 RangedWeapons[] 中的位置
    /// </summary>
    public class RangedWeapon : MonoBehaviour
    {
        [Header("投射物对象池")]
        [Tooltip("投射物预制体（用于创建对象池）")]
        [SerializeField] private Projectile _projectilePrefab;

        [Header("对象池参数")]
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxPoolSize = 20;

        /// <summary>
        /// 武器持有者（攻击者），在 Awake 中自动设置或外部注入
        /// </summary>
        public GameObject Owner { get; set; }

        /// <summary>
        /// 投射物发射方向覆盖。
        /// 在调用 Fire 前设置此值，可以让投射物朝指定方向飞行。
        /// 默认为 Vector3.zero，此时使用 transform.forward。
        /// 用于远程攻击时朝目标方向发射（如 EnemyVariable.Target）。
        /// </summary>
        public Vector3 OverrideDirection { get; set; }

        // ============ 投射物对象池 ============

        /// <summary>投射物对象池 — 减少 Instantiate/Destroy GC 抖动</summary>
        private IObjectPool<Projectile> _projectilePool;

        /// <summary>对象池是否已初始化</summary>
        private bool _poolInitialized;

        /// <summary>组件是否已被销毁</summary>
        private bool _isDestroyed;

        /// <summary>
        /// 初始化投射物对象池
        /// </summary>
        private void InitializeProjectilePool()
        {
            if (_poolInitialized || _projectilePrefab == null) return;

            _projectilePool = new ObjectPool<Projectile>(
                createFunc: () =>
                {
                    var proj = Instantiate(_projectilePrefab);
                    proj.SetPool(_projectilePool);
                    return proj;
                },
                actionOnGet: (p) => p.gameObject.SetActive(true),
                actionOnRelease: (p) => p.gameObject.SetActive(false),
                actionOnDestroy: (p) => Destroy(p.gameObject),
                collectionCheck: false,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxPoolSize
            );
            _poolInitialized = true;
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
        }

        /// <summary>
        /// 强制重置远程武器状态 — 由 BehaviourMachine 在状态切换或销毁时调用
        /// </summary>
        public void ResetWeapon()
        {
            OverrideDirection = Vector3.zero;
        }

        /// <summary>
        /// 发射投射物（远程攻击）— 由 AttackWeaponHelper.TryFireRangedAttack 调用
        /// 
        /// 方向优先级：OverrideDirection > transform.forward
        /// [优化] 优先使用对象池获取投射物，降低 Instantiate/Destroy GC 抖动。
        /// 如果对象池未初始化或不可用，回退到传统 Instantiate 方式。
        /// [安全] 使用 try/finally 确保 OverrideDirection 在任何情况下都被清零。
        /// 
        /// [单一数据源] 投射物预制体由 _projectilePrefab 统一管理，RangedHitWindow 不再配置预制体。
        /// </summary>
        /// <param name="window">远程攻击窗口配置（仅用于生成位置与时机）</param>
        public void Fire(RangedHitWindow window)
        {
            if (_isDestroyed) return;
            if (_projectilePrefab == null)
            {
                Debug.LogError($"[RangedWeapon] {name} 未设置 ProjectilePrefab，无法发射投射物！", this);
                return;
            }

            try
            {
                // 计算生成位置
                Vector3 spawnPos = transform.position;
                if (window.SpawnOffset != Vector3.zero)
                {
                    spawnPos = transform.TransformPoint(window.SpawnOffset);
                }

                // 方向：优先使用 OverrideDirection（指向目标），否则使用持有者的朝向
                Vector3 direction = OverrideDirection != Vector3.zero
                    ? OverrideDirection.normalized
                    : (Owner != null ? Owner.transform.forward : transform.forward);

                // 从对象池获取投射物
                InitializeProjectilePool();
                Projectile projectile;
                if (_projectilePool != null)
                {
                    projectile = _projectilePool.Get();
                    projectile.transform.position = spawnPos;
                }
                else
                {
                    // 池初始化失败，回退到 Instantiate
                    projectile = Object.Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
                }

                // 初始化投射物（注入攻击者 和 飞行方向）
                // [注意] Projectile.Initialize 内部会设置 transform.rotation = FromToRotation(Vector3.up, direction)
                // 因此 Fire 中无需额外设置旋转
                projectile.Initialize(Owner, direction);
            }
            finally
            {
                // [安全] 确保无论是否正常执行，方向都被清零
                OverrideDirection = Vector3.zero;
            }
        }
    }
}
