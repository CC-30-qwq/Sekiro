using UnityEngine;

namespace Sekiro.Character.Data
{
    /// <summary>
    /// 远程攻击专用判定窗口 — 定义投射物何时生成、生成偏移
    /// 每个 ComboConfigSO 可配置多个窗口，实现多段远程攻击（例如连射）
    /// 
    /// [解耦] 与 HitColliderWindow（近战）分离，仅包含远程必需的字段。
    /// 投射物预制体由 RangedWeapon 组件统一配置（单一数据源），避免重复配置。
    /// </summary>
    [System.Serializable]
    public struct RangedHitWindow
    {
        [Tooltip("选择使用 Machine.RangedWeapons 数组中的哪个远程武器触发此窗口。\n"
               + "-1 = 使用默认远程武器（RangedWeapons[0]），\n"
               + " 0 = RangedWeapons[0]，1 = RangedWeapons[1]，...")]
        public int WeaponIndex;

        [Tooltip("从攻击行为初始化起，多少秒后生成投射物（绝对值）\n"
               + "例: 0.2 = 动画开始后 0.2s 生成")]
        public float EnableTime;

        [Tooltip("相对于武器位置的生成偏移量\n"
               + "留空或 (0,0,0) 则在武器位置直接生成")]
        public Vector3 SpawnOffset;
    }
}
