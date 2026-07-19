namespace Sekiro.Character.Data
{
    /// <summary>
    /// 攻击类型枚举
    /// </summary>
    public enum AttackType { MeleeAttack = 0, SprintAttack = 1, ChargeAttack = 2, RangedAttack = 3 }

    /// <summary>
    /// 攻击阶段时序数据（值类型，零GC）
    /// 由 ComboConfigSO.ToAttackData() 构建，供 AttackPhaseDriver 使用
    /// </summary>
    [System.Serializable]
    public struct AttackData
    {
        public float ChargeTime;
        public float WindupTime;
        public float AttackDuration;
        public float RecoveryTime;
        public float ExitTime;

        public float TotalTime => ChargeTime + WindupTime + AttackDuration + RecoveryTime + ExitTime;
    }
}
