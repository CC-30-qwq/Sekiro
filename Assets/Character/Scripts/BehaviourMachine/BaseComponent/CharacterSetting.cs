using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 角色行为树的设置类，包含角色的各种配置数据
    /// </summary>
    [System.Serializable]
    public class CharacterSetting
    {
        public CapsuleColliderUtility ColliderUtility;
        public GroundCheckUtility GroundCheck;
    }
}
