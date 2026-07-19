using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 玩家行为枚举
    /// </summary>
    /// <remarks>
    /// 从 BehaviourEnum.cs 拆分至此独立文件，便于 Player 子系统维护。
    /// 命名空间保持 Sekiro.BehaviourMachine 以确保向后兼容。
    /// </remarks>
    public enum PlayerBehaviour
    {
        [DisplayName("Idle (待机)")]
        Idle = 0,
        [DisplayName("Start Run (起跑)")]
        StartRun = 1,
        [DisplayName("Start Sprint (起步冲刺)")]
        StartSprint = 2,
        [DisplayName("Run (跑步)")]
        Run = 3,
        [DisplayName("Sprint (冲刺)")]
        Sprint = 4,
        [DisplayName("Stop Start (停止-起跑)")]
        StopStart = 5,
        [DisplayName("Stop Run (停止-跑步)")]
        StopRun = 6,
        [DisplayName("Stop Sprint (停止-冲刺)")]
        StopSprint = 7,
        [DisplayName("Turn Start (转向-起跑)")]
        TurnStart = 8,
        [DisplayName("Turn Medium (转向-跑步)")]
        TurnMedium = 9,
        [DisplayName("Turn Hard (转向-冲刺)")]
        TurnHard = 10,
        [DisplayName("Jump Ready (跳跃预备-原地)")]
        StartJumpInPlace = 11,
        [DisplayName("Jump Ready (跳跃预备-移动)")]
        StartJumpDirect = 12,
        [DisplayName("Jump (跳跃-原地)")]
        JumpInPlace = 13,
        [DisplayName("Jump (跳跃-移动)")]
        JumpDirect = 14,
        [DisplayName("Fall (坠落)")]
        Fall = 15,
        [DisplayName("Land (着陆)")]
        StartLand = 16,
        [DisplayName("Land Idle (着陆-待机)")]
        LandInPlace = 17,
        [DisplayName("Land Run (着陆-移动)")]
        LandDirect = 18,
        [DisplayName("Dodge (闪避)")]
        Dodge = 19,
        [DisplayName("Attack (地面攻击)")]
        AttackGround = 20,
        [DisplayName("Attack (冲刺攻击)")]
        AttackSprint = 21,
        [DisplayName("Attack (空中攻击)")]
        AttackAir = 22,
        [DisplayName("Parry Start (招架预备)")]
        ParryStart = 23,
        [DisplayName("Parry Sprint (冲刺招架)")]
        ParrySprint = 24,
        [DisplayName("Parry 续招 0")]
        ParryReStart0 = 25,
        [DisplayName("Parry 续招 1")]
        ParryReStart1 = 26,
        [DisplayName("Defence (防御)")]
        Defence = 27,
        [DisplayName("Defence Move (防御移动)")]
        DefenceMove = 28,
        [DisplayName("Defence Stop (防御停止)")]
        DefenceMoveStop = 29,
        [DisplayName("Cancel Defence (取消防御)")]
        CancelDefence = 30,
        [DisplayName("Hit (受击)")]
        Hit = 31,
        [DisplayName("Block (格挡)")]
        Block = 32,
        [DisplayName("Deflect (弹反)")]
        Deflect = 33,
    }
}
