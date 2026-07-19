using System;
using System.Collections.Generic;
using Sekiro.BehaviourMachine;
using UnityEngine;
using UnityEngine.Pool;

namespace Sekiro.Combat
{
    /// <summary>
    /// 投射物组件 — 支持远程攻击（弓箭、子弹、能量弹等）
    /// 
    /// 由 RangedWeapon.Fire() 在 RangedHitWindow 配置的 EnableTime 时从对象池获取并生成，
    /// 自动向指定方向飞行，命中 IDamageable 后造成伤害并销毁。
    /// 
    /// [解耦] 投射物生成逻辑已从 Weapon（近战）迁移到独立的 RangedWeapon 组件管理。
    /// [优化] 支持对象池回收（UnityEngine.Pool.IObjectPool），减少 Instantiate/Destroy GC 抖动。
    /// 使用方式：
    /// 1. 在 RangedWeapon 组件上配置 ProjectilePrefab（单一数据源）
    /// 2. 在 Prefab 上挂载此组件并配置参数
    /// 3. 系统自动在 RangedHitWindow.EnableTime 时从池中获取并初始化
    /// 4. RangedWeapon 内部自动管理对象池生命周期
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        [Header("飞行参数")]
        [SerializeField] private float _speed = 20f;
        [SerializeField] private float _lifeTime = 5f;
        [SerializeField] private bool _destroyOnHit = true;

        [Header("伤害参数")]
        [SerializeField] private float _damage = 20f;
        [SerializeField] private DamageType _damageType;
        [SerializeField] private float _knockbackForce = 5f;

        [Header("穿透设置")]
        [Tooltip("可穿透的最大目标数量（0 = 不穿透）")]
        [SerializeField] private int _maxPenetrateCount = 0;
        [Tooltip("穿透后是否继续飞行")]
        [SerializeField] private bool _keepFlyingAfterPenetrate = true;

        [Header("攻击检测")]
        [SerializeField] private Collider _hitCollider;

        /// <summary>攻击者（由 Weapon 注入）</summary>
        private GameObject _owner;

        /// <summary>飞行方向（归一化向量）</summary>
        private Vector3 _direction;

        /// <summary>已命中的目标集合（防止同一目标多次伤害）</summary>
        private readonly HashSet<GameObject> _hitTargets = new();

        /// <summary>已穿透计数</summary>
        private int _penetrateCount;

        private float _elapsedTime;

        /// <summary>
        /// [安全] Destroy 延迟帧数 — 避免同一帧内 Destroy + OnTriggerEnter 竞态条件
        /// </summary>
        private const int DestroyDelayFrames = 3;

        // ============ 对象池支持 ============

        /// <summary>所属对象池（由 Weapon 或外部设置，使用后归还而非 Destroy）</summary>
        private IObjectPool<Projectile> _pool;

        /// <summary>
        /// 关联对象池并设置回收回调。
        /// 设置池后，组件将在生命周期结束或命中后调用 _pool.Release(this) 替代 Destroy。
        /// </summary>
        public void SetPool(IObjectPool<Projectile> pool)
        {
            _pool = pool;
        }

        /// <summary>
        /// [池回收] 将投射物归还对象池并重置状态
        /// </summary>
        private void ReleaseToPool()
        {
            if (_pool != null)
            {
                ResetState();
                _pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 重置投射物状态（对象池回收前调用，自动启用游戏对象）
        /// </summary>
        private void ResetState()
        {
            _owner = null;
            _direction = Vector3.zero;
            _elapsedTime = 0f;
            _penetrateCount = 0;
            _hitTargets.Clear();
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            _hitCollider = GetComponent<Collider>();
            if (_hitCollider != null)
            {
                _hitCollider.isTrigger = true;
            }
            else
            {
                Debug.LogError($"[Projectile] {name} 上未找到 Collider 组件！", this);
            }
        }

        /// <summary>
        /// 由 Weapon 在生成时调用，初始化投射物
        /// </summary>
        /// <param name="owner">攻击者（用于排除自伤）</param>
        /// <param name="direction">飞行方向</param>
        public void Initialize(GameObject owner, Vector3 direction)
        {
            _owner = owner;
            _direction = direction.normalized;
            _elapsedTime = 0f;
            _hitTargets.Clear();
            _penetrateCount = 0;

            // 头顶朝向飞行方向（投射物的 Y 轴指向 _direction）
            if (_direction != Vector3.zero)
            {
                transform.rotation = Quaternion.FromToRotation(Vector3.up, _direction);
            }

            // 从对象池取出时主动启用
            if (_pool != null && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;

            // 生命周期到期自动销毁/回收
            if (_elapsedTime >= _lifeTime)
            {
                ReleaseToPool();
                return;
            }

            // 飞行
            if (_direction != Vector3.zero)
            {
                transform.position += _direction * _speed * Time.deltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!enabled) return;

            // [安全] Destroy 延迟帧守卫：Unity 的 Destroy(gameObject) 是延迟执行的，
            // Destroy 后同一帧仍可能触发 OnTriggerEnter，此时组件字段已不可信。
            if (this == null || gameObject == null) return;

            // 排除自己和持有者
            if (other.gameObject == gameObject) return;
            if (_owner != null && other.gameObject == _owner) return;

            // 防止重复命中同一目标
            if (_hitTargets.Contains(other.gameObject)) return;

            // 尝试造成伤害
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                // 计算击退方向
                Vector3 dir = transform.up; // 投射物的 Y 轴方向
                dir.y = 0f;

                DamageInfo damage = new DamageInfo(
                    _damage, _damageType, _owner ?? gameObject,
                    dir, _knockbackForce
                );
                damageable.TakeDamage(damage);

                _hitTargets.Add(other.gameObject);
                _penetrateCount++;

                // 穿透限制
                if (_maxPenetrateCount > 0 && _penetrateCount >= _maxPenetrateCount)
                {
                    ReleaseToPool();
                    return;
                }

                // 命中后是否销毁
                if (_destroyOnHit && !_keepFlyingAfterPenetrate)
                {
                    ReleaseToPool();
                    return;
                }
            }
            else
            {
                // 撞到非伤害目标（墙壁等）也销毁
                // 但如果是触发器则只记录不销毁（可能用于触发其他逻辑）
                if (!other.isTrigger)
                {
                    ReleaseToPool();
                }
            }
        }

        private void OnDestroy()
        {
            _hitTargets.Clear();
            _pool = null;
        }
    }
}

