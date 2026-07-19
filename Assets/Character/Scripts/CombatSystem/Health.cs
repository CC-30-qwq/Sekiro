using System;
using UnityEngine;

namespace Sekiro.Combat
{
    /// <summary>
    /// 伤害类型枚举
    /// </summary>
    public enum DamageType { Physical, Fire, Magic, Poison }

    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public GameObject Character;
        public Vector3 Direction;
        public float Force;

        public DamageInfo(float amount, DamageType type, GameObject character, Vector3 direction, float force)
        {
            this.Amount = amount;
            this.Type = type;
            this.Character = character;
            this.Direction = direction;
            this.Force = force;
        }
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo damageInfo);
        bool IsAlive { get; }
    }

    /// <summary>
    /// 抗性配置 — 每种伤害类型的减免百分比（0~1）
    /// 例如 Physical = 0.3f 表示减免 30% 物理伤害
    /// </summary>
    [System.Serializable]
    public class ResistanceProfile
    {
        [Range(0f, 1f)]
        [SerializeField] private float _physical = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float _fire = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float _magic = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float _poison = 0f;

        /// <summary>获取指定伤害类型的抗性值（0~1）</summary>
        public float GetResistance(DamageType type)
        {
            return type switch
            {
                DamageType.Physical => _physical,
                DamageType.Fire     => _fire,
                DamageType.Magic    => _magic,
                DamageType.Poison   => _poison,
                _ => 0f,
            };
        }
    }

    public class Health : MonoBehaviour, IDamageable
    {
        [Header("基础属性")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth;

        [Header("伤害抗性")]
        [Tooltip("基础伤害抗性（0~1），直接乘法减免受到的伤害")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseDamageResistance = 0f;

        [Header("抗性（按伤害类型百分比减免）")]
        [SerializeField] private ResistanceProfile _resistances;

        // 运行时额外抗性修正（由状态行为如 Parry 动态设置）
        private float _resistanceModifier = 0f;

        // 事件（连接UI/动画）
        public event Action<float, float> OnHealthChanged; // (当前血量, 最大血量)
        public event Action<DamageInfo> OnDamaged;         // 受伤时触发（原始伤害信息）
        public event Action<DamageInfo> OnAfterResistance; // 抗性减免后触发（最终伤害信息）
        public event Action OnDeath;                       // 死亡时触发

        public bool IsAlive => _currentHealth > 0f;
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;

        /// <summary>总抗性 = 基础抗性 + 运行时修正（钳制 0~1）</summary>
        public float TotalResistance => Mathf.Clamp01(_baseDamageResistance + _resistanceModifier);

        /// <summary>
        /// 运行时额外抗性修正（范围为 0~1），由状态行为（如 Parry）动态设置。
        /// 例如设置为 0.7f 表示在该状态下额外减免 70% 伤害。
        /// </summary>
        public float DamageResistanceModifier
        {
            get => _resistanceModifier;
            set => _resistanceModifier = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        private void Start()
        {
            // 初始化时强制刷新一次UI
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void OnDestroy()
        {
            // [安全] 清理所有事件订阅，防止 dangling 引用和内存泄漏
            // 当 Health 组件被销毁时（如对象池回收/场景卸载），
            // 如果订阅者没有取消订阅，它们仍持有对已销毁 Health 的引用，
            // 调用 Invoke 时可能导致 NRE 或其他意外行为。
            OnDamaged = null;
            OnHealthChanged = null;
            OnAfterResistance = null;
            OnDeath = null;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            // —— 死亡保护：已死亡角色不再处理伤害 ——
            if (!IsAlive) return;

            // —— 通用抗性减伤（基础抗性 + 运行时修正，乘法） ——
            float finalDamage = damageInfo.Amount * (1f - TotalResistance);

            // —— 类型抗性减伤（按伤害类型百分比减免） ——
            float typeResistance = _resistances != null
                ? _resistances.GetResistance(damageInfo.Type)
                : 0f;
            finalDamage = Mathf.Max(finalDamage * (1f - typeResistance), 0f);

            _currentHealth -= finalDamage;

            // 构造最终伤害信息（Amount 已减免）
            DamageInfo finalDamageInfo = new DamageInfo(
                finalDamage, damageInfo.Type, damageInfo.Character,
                damageInfo.Direction, damageInfo.Force
            );

            // 【事件触发顺序约定】
            //   1. OnDamaged / OnAfterResistance — 受伤回调（订阅者可访问 IsAlive 判断是否为致命伤）
            //   2. OnHealthChanged — 血量变更回调（UI 更新等）
            //   3. OnDeath — 死亡回调（订阅者可做对象池回收、销毁等清理）
            // 
            // 死亡事件在最后触发，确保 OnDamaged/OnHealthChanged 中订阅者访问状态时
            // 数据仍然有效，不会因 OnDeath 中的清理逻辑导致空引用。

            // 步骤 1：触发受伤事件（动画、音效、受击特效等）
            OnDamaged?.Invoke(damageInfo);
            OnAfterResistance?.Invoke(finalDamageInfo);

            // 步骤 2：触发血量变更事件（UI 更新等）
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            // 步骤 3：死亡判定 — 在血量变更事件之后触发
            if (!IsAlive)
            {
                _currentHealth = 0f;
                OnDeath?.Invoke();
            }
        }


        public void Heal(float amount)
        {
            if (!IsAlive) return;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
