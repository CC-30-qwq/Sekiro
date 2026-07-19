using System.Buffers;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 地面检测工具类 - 从 BehaviourMethods 中提取的独立职责
    /// 提供高效的地面检测和悬浮物理处理
    /// </summary>
    public static class GroundDetectionUtility
    {
        #region Ground Check Methods

        /// <summary>
        /// 执行高效的地面检测，使用两阶段检测策略确保检测可靠性
        /// </summary>
        /// <remarks>
        /// 使用两阶段检测策略：
        /// 1. 首先使用中心射线检测获取精确的地面信息（命中点和法线）
        /// 2. 如果中心检测失败，使用圆形重叠检测作为备用方案
        /// 
        /// 性能优化特性：
        /// - 使用 ArrayPool 租用的碰撞器数组避免 GC 分配，同时消除多角色共享静态数组的数据竞争
        /// - 中心射线检测提供精确的命中点和法线信息
        /// - 圆形检测确保在斜坡或不平坦地面的可靠性
        /// </remarks>
        public static void GroundCheck(
            Transform transform,
            CharacterSetting setting,
            CharacterVariable variable)
        {
            // Null 安全检查：防止 SerializeField 字段在 Inspector 未配置时导致 NullReferenceException
            if (setting.ColliderUtility == null || setting.GroundCheck == null)
            {
                Debug.LogError($"GroundDetectionUtility.GroundCheck: ColliderUtility or GroundCheck is null on {transform.name}. " +
                    "Ensure the fields are assigned in the CharacterSetting inspector.");
                return;
            }

            var colliderUtility = setting.ColliderUtility;
            var groundCheck = setting.GroundCheck;

            bool rayHit = Physics.Raycast(
                colliderUtility.CapsuleCollider.bounds.center,
                Vector3.down,
                out variable.GroundRayHit,
                colliderUtility.FloatRayDistance,
                groundCheck.GroundLayer
            );

            // 中心Raycast失败时，使用圆形检测作为备用
            int overlapCount = 0;
            if (!rayHit)
            {
                // 从 ArrayPool 租用碰撞器数组 — 避免多角色共享同一静态数组的数据竞争
                // 且不产生 GC 分配（ArrayPool 复用底层数组）
                Collider[] colliders = ArrayPool<Collider>.Shared.Rent(8);
                try
                {
                    overlapCount = Physics.OverlapSphereNonAlloc(
                        transform.position + groundCheck.SphereCheckOffset,
                        groundCheck.SphereCheckRadius,
                        colliders,
                        groundCheck.GroundLayer
                    );
                }
                finally
                {
                    // 使用完后归还到池中
                    ArrayPool<Collider>.Shared.Return(colliders);
                }
            }

            variable.IsGrounded = rayHit || overlapCount > 0;
            variable.GroundPoint = variable.IsGrounded
                ? (rayHit ? variable.GroundRayHit.point : transform.position)
                : variable.GroundPoint;
        }


        /// <summary>
        /// 执行悬浮物理处理，保持角色与地面的稳定距离
        /// </summary>
        /// <remarks>
        /// 使用两阶段检测策略确保在复杂地形上的稳定性：
        /// 1. 首先尝试中心射线检测获取精确的地面信息
        /// 2. 如果中心检测失败，使用圆形分布的射线阵列进行全方位检测
        /// 
        /// 性能优化：
        /// - 使用预分配的射线起点计算，避免每帧分配新向量
        /// - 只计算最近的命中点，减少不必要的物理计算
        /// - 根据命中距离动态调整悬浮力，实现平滑的悬浮效果
        /// </remarks>
        public static void Float(
            Transform transform,
            Rigidbody rigidbody,
            CharacterSetting setting)
        {
            // Null 安全检查：防止 SerializeField 字段在 Inspector 未配置时导致 NullReferenceException
            if (setting.ColliderUtility == null || setting.GroundCheck == null)
            {
                Debug.LogError($"GroundDetectionUtility.Float: ColliderUtility or GroundCheck is null on {transform.name}. " +
                    "Ensure the fields are assigned in the CharacterSetting inspector.");
                return;
            }

            var colliderUtility = setting.ColliderUtility;
            var groundCheck = setting.GroundCheck;
            float rayDist = colliderUtility.FloatRayDistance + 0.01f;

            bool rayHit = Physics.Raycast(
                colliderUtility.CapsuleCollider.bounds.center,
                Vector3.down,
                out RaycastHit hit,
                rayDist,
                groundCheck.GroundLayer
            );

            float closestDist = float.MaxValue;

            // 中心Raycast失败时，使用圆形射线检测作为备用
            if (!rayHit)
            {
                Vector3 sphereCenter = colliderUtility.CapsuleCollider.bounds.center;
                float radius = groundCheck.SphereCheckRadius;
                int rayCount = colliderUtility.CircleRayCount;

                // 在圆形区域内进行多个射线检测，找到最近的命中点
                // 使用可配置数量的射线均匀分布在圆周上
                for (int i = 0; i < rayCount; i++)
                {
                    float angle = (360f / rayCount) * i;
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                    Vector3 rayOrigin = sphereCenter + direction * radius;

                    if (Physics.Raycast(
                        rayOrigin,
                        Vector3.down,
                        out RaycastHit circleHit,
                        rayDist,
                        groundCheck.GroundLayer))
                    {
                        if (circleHit.distance < closestDist)
                        {
                            closestDist = circleHit.distance;
                            hit = circleHit;
                        }
                    }
                }

                rayHit = closestDist < float.MaxValue;
            }

            if (rayHit)
            {
                float dist = colliderUtility.ColliderCenterInLocalSpace.y * transform.localScale.y - hit.distance;

                float force = dist * colliderUtility.StepReachForce - rigidbody.velocity.y;
                // Vector3 是值类型，栈上分配，零GC
                rigidbody.AddForce(new Vector3(0, force, 0), ForceMode.VelocityChange);
            }
        }

        #endregion
    }
}
