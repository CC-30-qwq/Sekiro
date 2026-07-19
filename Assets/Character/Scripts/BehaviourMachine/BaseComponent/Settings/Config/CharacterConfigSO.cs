using UnityEngine;

namespace Sekiro.Character.Data
{
    /// <summary>
    /// 角色配置资产 — 基础移动/战斗属性
    /// 替代旧的 PlayerStatus/CharacterStatus 可序列化类，
    /// 支持跨角色复用和编辑器下独立资产管理
    /// </summary>
    [CreateAssetMenu(menuName = "Sekiro/Character/Config", fileName = "CharConfig_")]
    public class CharacterConfigSO : ScriptableObject
    {
        [Header("移动参数")]
        public float ModifyMoveSpeed = 1f;
        public float MoveSlerp = 1f;
        public float NormalRotateSpeed = 5f;
        public float SprintRotateSpeed = 5f;
        public float AirRotateSpeed = 5f;
        public float JumpHeight = 2f;
        public float JumpForce = 5f;
        public float FallForce = 5f;
        public float AirMoveSpeed = 5f;
        public float AirDragSpeed = 5f;
        public float GravityMultiplier = 1f;

        [Header("战斗参数")]
        public float MaxHealth = 100f;
        public float KnockbackForceMultiplier = 1f;
        public ForceMode KnockbackForceMode = ForceMode.Impulse;

        [Header("动画参数")]
        public float AnimParameterLerp = 5f;
    }
}
