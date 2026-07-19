using System;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 为枚举值提供友好显示名称的属性
    /// 替代 TransitionNode.GetDisplayName 中的巨大 switch 块
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class DisplayNameAttribute : Attribute
    {
        public string DisplayName { get; }

        public DisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
