using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 武器管理组件 — 拥有 Weapons/RangedWeapons 数组，处理 Owner 赋值与生命周期清理。
    /// 从 BehaviourMachine 抽离，减小子类体量。
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [field: SerializeField] public Weapon[] Weapons { get; private set; }

        [field: SerializeField]
        private RangedWeapon[] _rangedWeapons;
        public RangedWeapon[] RangedWeapons
        {
            get => _rangedWeapons;
            private set => _rangedWeapons = value;
        }

        private void Awake()
        {
            if (Weapons != null)
            {
                foreach (var weapon in Weapons)
                {
                    if (weapon != null)
                        weapon.Owner = gameObject;
                }
            }

            if (RangedWeapons != null)
            {
                foreach (var rangedWeapon in RangedWeapons)
                {
                    if (rangedWeapon != null)
                        rangedWeapon.Owner = gameObject;
                }
            }
        }

        private void OnDestroy()
        {
            if (Weapons != null)
            {
                for (int i = 0; i < Weapons.Length; i++)
                {
                    if (Weapons[i] != null && Weapons[i].gameObject != null)
                        Weapons[i].ResetWeapon();
                }
            }

            if (RangedWeapons != null)
            {
                for (int i = 0; i < RangedWeapons.Length; i++)
                {
                    if (RangedWeapons[i] != null && RangedWeapons[i].gameObject != null)
                        RangedWeapons[i].ResetWeapon();
                }
            }
        }
    }
}
