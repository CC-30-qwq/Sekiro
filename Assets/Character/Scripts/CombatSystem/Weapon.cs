using System.Collections;
using System.Collections.Generic;
using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.Combat
{
    /// <summary>
    /// 近战武器组件 — 负责管理近战碰撞体的启用时序和伤害检测
    /// 
    /// 职责：
    /// - 管理多段 HitCollider 窗口协程时序
    /// - 在触发时启用/禁用碰撞体
    /// - 检测碰撞并造成伤害
    /// 
    /// [解耦] 远程攻击已独立到 RangedWeapon + RangedHitWindow，
    /// 此组件仅处理近战逻辑，不再包含投射物相关代码。
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("伤害参数")]
        [SerializeField] private float _baseDamage = 20f;
        [SerializeField] private DamageType _damageType;
        [SerializeField] private float _knockbackForce = 5f;

        [Header("攻击检测")]
        [SerializeField] private Collider _hitCollider;   // 武器上的碰撞体（通常初始禁用）

        /// <summary>
        /// 武器持有者（攻击者），在 Awake 中自动设置或外部注入
        /// </summary>
        public GameObject Owner { get; set; }

        private readonly HashSet<GameObject> _hitTargets = new();

        #region Coroutine Management

        /// <summary>
        /// [安全] 协程跟踪器 — 强引用包装
        /// 保存 Coroutine 和 IEnumerator 引用，避免 WeakReference 导致的 GC 回收问题
        /// </summary>
        private class CoroutineTracker
        {
            public Coroutine Coroutine { get; set; }
            /// <summary>保存 IEnumerator 引用，防止 StartCoroutine 内部引用被 GC 回收</summary>
            public IEnumerator Enumerator { get; set; }
        }

        /// <summary>
        /// 管理多段 HitCollider 窗口的协程列表
        /// 每个窗口启动一个协程：等待 EnableTime → 启用 → 等待 Duration → 禁用
        /// 所有协程共享 _hitCollider，由 CoroutineTracker 跟踪生命周期，协程结束自动移除
        /// </summary>
        private readonly List<CoroutineTracker> _windowCoroutines = new();

        /// <summary>组件是否已被销毁</summary>
        private bool _isDestroyed;

        #endregion

        private void Awake()
        {
            _hitCollider ??= GetComponent<Collider>();
            if (_hitCollider != null)
            {
                _hitCollider.enabled = false;
            }
            else
            {
                Debug.LogError($"[Weapon] {name} 上未找到 Collider 组件！", this);
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            _hitTargets.Clear();
            StopAllHitboxWindows();
        }

        /// <summary>
        /// 强制重置武器状态 — 由 BehaviourMachine 在状态切换或销毁时调用
        /// 清理击中记录和协程，避免内存泄漏
        /// </summary>
        public void ResetWeapon()
        {
            _hitTargets.Clear();
            StopAllHitboxWindows();
        }

        /// <summary>
        /// 执行多段近战 HitCollider 攻击判定
        /// 遍历 ComboConfigSO.HitColliderWindows，为每个窗口启动独立协程
        /// 自动过滤：
        /// - WeaponIndex == -1 的窗口由默认武器（索引0）处理
        /// - WeaponIndex == thisIndex 的窗口由本武器处理
        /// - 其他索引的窗口跳过
        /// </summary>
        /// <param name="windows">HitCollider 窗口数组，每个窗口独立控制 EnableTime + Duration</param>
        /// <param name="elapsedTime">当前 Attacking 阶段已消逝时间（用于消除双重等待）</param>
        /// <param name="thisIndex">本武器在 Machine.Weapons 数组中的索引</param>
        public void ExecuteAttack(HitColliderWindow[] windows, float elapsedTime, int thisIndex = -1)
        {
            if (windows == null || windows.Length == 0) return;

            // 停止所有正在运行的窗口协程
            StopAllHitboxWindows();

            // 为每个窗口启动独立协程，跳过指定给其他武器的窗口
            for (int i = 0; i < windows.Length; i++)
            {
                var window = windows[i];

                // 只处理武器索引匹配的窗口：
                // - WeaponIndex == -1 交由默认武器（索引0）处理
                // - WeaponIndex == thisIndex 由本武器处理
                int targetIndex = window.WeaponIndex;
                if (targetIndex != -1 && targetIndex != thisIndex) continue;

                // 计算剩余等待时间：总 EnableTime - 已消逝时间
                // 如果剩余 <= 0，则立即启用（无需 yield）
                float remaining = window.EnableTime - elapsedTime;

                // 创建协程跟踪器并启动协程 — 协程通过 finally 块自移除
                var tracker = new CoroutineTracker();
                tracker.Enumerator = HitboxWindowRoutine(tracker, remaining, window);
                tracker.Coroutine = StartCoroutine(tracker.Enumerator);
                _windowCoroutines.Add(tracker);
            }
        }

        /// <summary>
        /// 停止所有活跃的 HitCollider 窗口协程并强制禁用碰撞体
        /// </summary>
        private void StopAllHitboxWindows()
        {
            for (int i = _windowCoroutines.Count - 1; i >= 0; i--)
            {
                var tracker = _windowCoroutines[i];
                if (tracker?.Coroutine != null)
                    StopCoroutine(tracker.Coroutine);
            }
            _windowCoroutines.Clear();
            if (_hitCollider != null)
                _hitCollider.enabled = false;
        }

        /// <summary>
        /// 从协程跟踪列表中移除指定跟踪器（协程结束或取消时调用）
        /// </summary>
        private void RemoveCoroutineTracker(CoroutineTracker tracker)
        {
            int lastIndex = _windowCoroutines.Count - 1;
            int index = _windowCoroutines.IndexOf(tracker);
            if (index >= 0)
            {
                // 交换移除法 — O(1) 操作，无需后续元素移位
                _windowCoroutines[index] = _windowCoroutines[lastIndex];
                _windowCoroutines.RemoveAt(lastIndex);
            }
        }

        /// <summary>
        /// 单个 HitCollider 窗口协程：
        /// 等待剩余时间 → 启用碰撞体 → 等待 Duration → 禁用
        /// 
        /// [解耦] 远程攻击分支已移除，此协程仅处理近战逻辑。
        /// 远程攻击由 RangedWeapon.Fire() 通过 AttackWeaponHelper 独立管理。
        /// 
        /// [安全] 通过 try/finally 确保无论何种方式结束，跟踪器都从列表中移除，
        /// 避免 _windowCoroutines 列表积累陈旧引用导致的内存泄漏。
        /// </summary>
        /// <param name="tracker">协程跟踪器，用于在 finally 中自移除</param>
        /// <param name="remainingTime">剩余等待时间（总 EnableTime - 已消逝的 CurrentPhaseTime）</param>
        /// <param name="window">HitCollider 窗口配置</param>
        private IEnumerator HitboxWindowRoutine(CoroutineTracker tracker, float remainingTime, HitColliderWindow window)
        {
            try
            {
                // 阶段 1：等待剩余的启用时间（如果 GameObject 已销毁或剩余时间 <= 0 则直接跳过）
                if (remainingTime > 0f)
                    yield return new WaitForSeconds(remainingTime);

                // 协程恢复后检查组件是否已被销毁，避免操作已销毁的组件
                if (_isDestroyed) yield break;

                // ====== 近战攻击：启用武器碰撞体 ======
                if (_hitCollider == null) yield break;
                _hitTargets.Clear();
                _hitCollider.enabled = true;

                if (window.Duration > 0f)
                    yield return new WaitForSeconds(window.Duration);

                if (_isDestroyed) yield break;

                if (_hitCollider != null)
                    _hitCollider.enabled = false;
            }
            finally
            {
                // [安全] 无论正常结束、yield break、还是被 StopCoroutine 终止，
                // 都确保从协程列表中移除跟踪器，防止陈旧的引用累积。
                if (!_isDestroyed && tracker != null)
                {
                    RemoveCoroutineTracker(tracker);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hitCollider == null || !_hitCollider.enabled) return;

            // 排除自己
            if (other.gameObject == gameObject) return;

            // 排除持有者 - 双重判空防止 MissingReferenceException（Unity对象销毁后 != null 但实际不可用）
            if (Owner == null || !Owner) return;
            if (other.gameObject == Owner) return;

            if (_hitTargets.Contains(other.gameObject)) return;

            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                // 计算击退方向：从攻击者指向受击者
                Vector3 dir = (other.transform.position - Owner.transform.position).normalized;
                dir.y = 0f; // 如果是3D且希望水平击退
                DamageInfo damage = new DamageInfo(
                    _baseDamage, _damageType, gameObject,
                    dir, _knockbackForce
                );
                damageable.TakeDamage(damage);

                _hitTargets.Add(other.gameObject);
            }
        }
    }
}
