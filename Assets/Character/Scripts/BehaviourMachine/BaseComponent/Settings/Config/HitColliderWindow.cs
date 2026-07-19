using UnityEngine;

namespace Sekiro.Character.Data
{
    /// <summary>
    /// HitCollider 判定窗口 — 定义近战攻击碰撞体何时启用、持续多久
    /// 每个 ComboConfigSO 可配置多个窗口，实现多段判定的效果
    /// 
    /// 支持为每个窗口指定使用的武器索引（WeaponIndex），实现多武器多段判定：
    /// - -1 = 使用默认武器（Machine.Weapons[0]）
    /// - 0/1/2... = 对应 Machine.Weapons 数组中的索引
    /// 
    /// [解耦] 远程攻击已独立到 RangedHitWindow + RangedWeapon，
    /// 此结构仅保留近战需要的字段。
    /// </summary>
    [System.Serializable]
    public struct HitColliderWindow
    {
        [Tooltip("选择使用 Machine.Weapons 数组中的哪个武器触发此窗口。\n"
               + "-1 = 使用默认武器（Weapons[0]），\n"
               + " 0 = Weapons[0]，1 = Weapons[1]，...")]
        public int WeaponIndex;

        [Tooltip("从攻击行为初始化起，多少秒后启用 HitCollider（绝对值）\n"
               + "例: 0.2 = 动画开始后 0.2s 启用，自动跨越 Charging/Windup 阶段")]
        public float EnableTime;

        [Tooltip("HitCollider 启用后持续多少秒后自动禁用")]
        public float Duration;
    }
}
