using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 角色变量数据类，存储角色的运行时状态
    /// </summary>
    [System.Serializable]
    public class CharacterVariable
    {
        #region Animation

        /// <summary>
        /// 传递给Animator的参数向量（X和Y分量）
        /// </summary>
        [field: Header("Animation")]
        [field: SerializeField]
        public Vector2 AnimatorParameterVector2 { get; set; }

        /// <summary>
        /// 经过归一化处理的输入方向向量
        /// </summary>
        [field: SerializeField]
        public Vector2 AnimInputValue { get; set; }

        /// <summary>
        /// 目标移动方向向量
        /// </summary>
        [field: SerializeField]
        public Vector2 TargetVector { get; set; }

        #endregion

        #region Movement

        /// <summary>
        /// 根运动移动方向（由动画驱动）
        /// </summary>
        [field: Header("Movement")]
        [field: SerializeField]
        public Vector3 RootMovePosition { get; set; }

        /// <summary>
        /// 角色面向的方向
        /// </summary>
        [field: SerializeField]
        public Quaternion FaceDirection { get; set; }

        /// <summary>
        /// 旋转速度（度/秒）
        /// </summary>
        [field: SerializeField]
        public float RotateSpeed { get; set; }

        /// <summary>
        /// 垂直方向速度（Y轴方向）
        /// </summary>
        [field: SerializeField]
        public float VerticalSpeed { get; set; }

        /// <summary>
        /// 跳跃方向向量（X和Z分量）
        /// </summary>
        public Vector2 JumpDirection { get; set; }

        /// <summary>
        /// 跳跃力量值
        /// </summary>
        public float HorizontalSpeed { get; set; }

        #endregion

        #region Ground Check

        /// <summary>
        /// 角色是否站立在地面上
        /// </summary>
        [field: Header("Ground Check")]
        public bool IsGrounded { get; set; }

        /// <summary>
        /// 最近的地面接触点
        /// </summary>
        public Vector3 GroundPoint { get; set; }

        /// <summary>
        /// 地面射线检测结果（保持字段而非属性，因为 Physics.Raycast 的 out 参数需要字段引用）
        /// 使用 internal 而非 public 防止外部模块直接修改，仅在当前程序集内可见
        /// </summary>
        internal RaycastHit GroundRayHit;

        #endregion

        #region Combat

        /// <summary>
        /// 当前连击配置（使用 SO 直接引用替代整数索引）
        /// 不再依赖 ComboDatabase 中的列表顺序
        /// </summary>
        [field: Header("Combat")]
        [field: SerializeField]
        public ComboConfigSO CurrentCombo { get; set; }

        /// <summary>
        /// 当前帧是否受击
        /// Behaviour 在 OnEnter 中手动重置为 false
        /// </summary>
        public bool IsHit { get; set; }

        /// <summary>
        /// 最近一次受击方向
        /// </summary>
        public Vector3 HitDirection { get; set; }

        [field: Header("Target")]
        [field: SerializeField]
        public Transform Target;

        [field: SerializeField]
        public float CachedDistance { get; set; } = 0f;

        [field: SerializeField]
        public Vector3 TargetDirection { get; set; }

        #endregion

    }
}
