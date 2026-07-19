namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 敌人行为枚举
    /// </summary>
    /// <remarks>
    /// 从 BehaviourEnum.cs 拆分至此独立文件，便于 Enemy 子系统维护。
    /// 命名空间保持 Sekiro.BehaviourMachine 以确保向后兼容。
    /// </remarks>
    public enum EnemyBehaviour
    {
        Idle = 0,
        Stander = 1,
        Chase = 2,
        Return = 3,
        Attack0 = 4,
        Skill0 = 5,
        Skill1 = 6,
        Skill2 = 7,
        Skill3 = 8,
        Skill4 = 9,
        Skill5 = 10,
        SkillCounter = 11,
        SkillExtension = 12,
        Block = 13,
        Parry = 14,
        Dodge = 15,
        Hit = 16,
        Stagger = 17,
        Death = 18,
        Search = 19,
        Investigate = 20,
    }
}
