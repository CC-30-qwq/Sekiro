using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 胶囊体碰撞器工具类
    /// </summary>
    [System.Serializable]
    public class CapsuleColliderUtility
    {
        #region Properties

        public CapsuleCollider CapsuleCollider { get; private set; }
        public Vector3 ColliderCenterInLocalSpace { get; private set; }

        [field: SerializeField]
        public float Height { get; private set; }

        [field: SerializeField]
        public float CenterY { get; private set; }

        [field: SerializeField]
        public float Radius { get; private set; }

        [field: SerializeField]
        [field: Range(0f, 1f)]
        public float StepHeightPercentage { get; private set; }

        [field: SerializeField]
        [field: Range(0f, 5f)]
        public float FloatRayDistance { get; private set; }

        [field: SerializeField]
        [field: Range(0f, 50f)]
        public float StepReachForce { get; private set; }

        [field: SerializeField]
        [field: Range(1, 16)]
        public int CircleRayCount { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化碰撞器引用
        /// </summary>
        public void Initialize(GameObject go)
        {
            if (CapsuleCollider != null) return;
            CapsuleCollider = go.GetComponent<CapsuleCollider>();
            UpdateColliderData();
        }

        /// <summary>
        /// 更新碰撞器数据
        /// </summary>
        public void UpdateColliderData()
        {
            ColliderCenterInLocalSpace = CapsuleCollider.center;
        }

        /// <summary>
        /// 计算胶囊体碰撞器尺寸
        /// </summary>
        public void CalculateCapsuleColliderDimensions()
        {
            CapsuleCollider.radius = Radius;
            float adjustedHeight = Height * (1f - StepHeightPercentage);
            CapsuleCollider.height = adjustedHeight;

            float heightDiff = Height - CapsuleCollider.height;
            float newCenterY = CenterY + (heightDiff / 2f);
            CapsuleCollider.center = new Vector3(0f, newCenterY, 0f);

            float halfHeight = CapsuleCollider.height / 2f;
            if (halfHeight < CapsuleCollider.radius)
            {
                CapsuleCollider.radius = halfHeight;
            }

            UpdateColliderData();
        }

        #endregion
    }
}
