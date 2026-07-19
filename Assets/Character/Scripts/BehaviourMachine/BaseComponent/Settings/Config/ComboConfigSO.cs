using UnityEngine;

namespace Sekiro.Character.Data
{
    /// <summary>
    /// 连击配置资产 — 每个连击组合独立一个 SO，支持跨角色复用
    /// 
    /// 【连击链接说明】
    /// 使用 ComboConfigSO 直接引用替代旧的整数索引方案，避免连击列表顺序变动导致的链接断裂。
    /// 现在你可以随意在 ComboDatabaseSO 的列表中增删改顺序，无需手动调整链接索引。
    /// </summary>
    [CreateAssetMenu(menuName = "Sekiro/Combat/Combo Config", fileName = "Combo_")]
    public class ComboConfigSO : ScriptableObject
    {
        [Header("动画")]
        public string AnimationClipName;

        [HideInInspector] public int CachedAnimHash;

#if UNITY_EDITOR
        [Header("预览（仅编辑器）")]
        [Tooltip("在 Inspector 预览动画时使用的模型 Prefab。\n留空默认使用玩家模型 (Sekiro.fbx)")]
        public GameObject PreviewModel;

        [Tooltip("直接拖入动画剪辑用于预览，留空使用 AnimationClipName 自动查找")]
        public AnimationClip PreviewAnimationClip;
#endif

        [Header("攻击属性")]
        public AttackType AttackType;
        public int AttackStrength;

        public float ChargeTime;
        public float WindupTime;
        public float AttackDuration;
        public float RecoveryTime;
        public float ExitTime;

        [Header("近战 HitCollider 判定配置")]
        [Tooltip("多段近战 HitCollider 窗口数组。每个窗口独立控制启用时机和持续时间。\n"
               + "例如回旋斩可在 0.0s 和 0.5s 各设一段判定窗口")]
        public HitColliderWindow[] HitColliderWindows = new HitColliderWindow[]
        {
            new HitColliderWindow { EnableTime = 0f, Duration = 0.3f },
        };

        [Header("远程 HitCollider 判定配置")]
        [Tooltip("远程攻击投射物生成窗口数组。每个窗口独立控制投射物生成时机。\n"
               + "仅在 AttackType == RangedAttack 时生效。\n"
               + "[解耦] 与 HitColliderWindows 分离，字段独立")]
        public RangedHitWindow[] RangedHitWindows;

        [Header("连击链接")]
        [Tooltip("按攻击键后进入的下一个连击（拖拽 ComboConfigSO 资产到此）")]
        public ComboConfigSO NextComboOnAttack;

        [Tooltip("特殊条件进入的下一个连击，如蓄力释放后（拖拽 ComboConfigSO 资产到此）")]
        public ComboConfigSO NextComboOnRelease;

        /// <summary>
        /// 转换为 AttackData 供 AttackPhaseDriver 使用
        /// </summary>
        public AttackData ToAttackData()
        {
            return new AttackData
            {
                ChargeTime = ChargeTime,
                WindupTime = WindupTime,
                AttackDuration = AttackDuration,
                RecoveryTime = RecoveryTime,
                ExitTime = ExitTime
            };
        }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(AnimationClipName))
                CachedAnimHash = Animator.StringToHash(AnimationClipName);
        }
    }
}
