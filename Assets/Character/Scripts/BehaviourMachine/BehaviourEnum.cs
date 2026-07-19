namespace Sekiro.BehaviourMachine
{
    // ============ 枚举定义 ============
    //
    // [重构说明] PlayerBehaviour 和 EnemyBehaviour 已拆分为独立文件：
    //   - Player/Enums/PlayerBehaviour.cs
    //   - Enemy/Enums/EnemyBehaviour.cs
    // 命名空间保持 Sekiro.BehaviourMachine 以确保零迁移成本。
    // 此处仅保留共享枚举类型（ParryState, AttackState, DistanceRange）。
    // 如需修改 PlayerBehaviour 或 EnemyBehaviour，请编辑对应独立文件。


    /// <summary>
    /// 招架阶段枚举 — Windup → Parrying → Recovery 三段时序
    /// </summary>
    public enum ParryState { Windup = 0, Parrying = 1, Recovery = 2 }

    /// <summary>
    /// 攻击状态枚举
    /// </summary>
    public enum AttackState { Charging = 0, Windup = 1, Attacking = 2, Recovery = 3, Exit = 4 }

    /// <summary>
    /// 距离范围枚举
    /// </summary>
    public enum DistanceRange { Close = 0, Mid = 1, Far = 2, OutOfRange = 3 }
}
