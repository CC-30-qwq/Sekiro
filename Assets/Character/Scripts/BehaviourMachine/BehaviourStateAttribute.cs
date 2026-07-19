using System;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// [可扩展性] 行为状态特性标记
    /// 标记在具体行为类上，指定其对应的枚举值。
    /// 配合 PlayerBehaviourMachine.AutoRegisterStates() 实现零硬编码状态注册。
    /// 
    /// 使用方式:
    /// <code>
    /// [BehaviourState(PlayerBehaviour.Idle)]
    /// public class IdleBehaviour : PlayerBaseBehaviour { ... }
    /// </code>
    /// 
    /// 注意：中间基类（如 CombatBehaviour）不应标记此特性，
    /// 只有叶子（具体）行为类应标记。
    /// 多个行为类映射到同一枚举值时，后注册的覆盖先注册的。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class BehaviourStateAttribute : Attribute
    {
        /// <summary>
        /// 对应的行为枚举值
        /// </summary>
        public Enum BehaviourType { get; }

        /// <summary>
        /// 标记具体行为类关联的枚举值（支持 PlayerBehaviour / EnemyBehaviour）
        /// </summary>
        /// <param name="behaviourType">行为枚举值（如 PlayerBehaviour.Idle）</param>
        public BehaviourStateAttribute(object behaviourType)
        {
            if (behaviourType is Enum)
            {
                BehaviourType = (Enum)behaviourType;
            }
            else
            {
                throw new ArgumentException("BehaviourType must be an enum value.");
            }
        }
    }
}
