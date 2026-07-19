using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 根运动工具类 - 从 BehaviourMethods 中提取的独立职责
    /// 提供三种不同的根运动应用方式（原始/修改/方向限制）
    /// </summary>
    public static class RootMotionUtility
    {
        /// <summary>
        /// 更新角色的根运动位置，应用基于动画的根运动位移
        /// </summary>
        /// <remarks>
        /// 此方法将 rootMovePosition 中存储的根运动位移应用到角色的Rigidbody上。
        /// 使用平方幅度比较（sqrMagnitude > Constants.VectorNearZeroThreshold）代替向量零值比较，避免平方根计算开销。
        /// 根运动位移通常来自动画系统（OnAnimatorMove事件），提供自然的移动效果。
        /// 此版本将位移投影到角色的前/右方向上。
        /// </remarks>
        public static void UpdateModifyRootPosition(Rigidbody rigidbody, Vector3 rootMovePosition)
        {
            // 位移过小则忽略
            if (rootMovePosition.sqrMagnitude <= Constants.VectorNearZeroThreshold)
                return;

            // 获取角色的前、右方向（忽略Y轴，保持在水平面）
            Vector3 forward = rigidbody.transform.forward;
            Vector3 right = rigidbody.transform.right;
            forward.Normalize();
            right.Normalize();

            // 计算位移在前后、左右轴上的投影长度（带符号）
            float dotForward = Vector3.Dot(rootMovePosition, forward);
            float dotRight = Vector3.Dot(rootMovePosition, right);

            // 比较绝对值，决定最终方向
            Vector3 finalMove;
            if (Mathf.Abs(dotForward) >= Mathf.Abs(dotRight))
            {
                // 前后方向：保留符号（正为前，负为后）
                finalMove = forward * dotForward;
            }
            else
            {
                // 左右方向：保留符号（正为右，负为左）
                finalMove = right * dotRight;
            }

            Vector3 targetPos = rigidbody.position + finalMove;
            rigidbody.MovePosition(targetPos);
        }

        /// <summary>
        /// 更新根运动位置 - 保留原始位移方向，仅忽略Y轴
        /// </summary>
        public static void UpdateOriginRootPosition(Rigidbody rigidbody, Vector3 rootMovePosition)
        {
            if (rootMovePosition.sqrMagnitude > Constants.VectorNearZeroThreshold)
            {
                // Vector3 是值类型，栈上分配，零GC
                Vector3 horizontalMove = new Vector3(rootMovePosition.x, 0, rootMovePosition.z);
                Vector3 rootMotion = rigidbody.position + horizontalMove;
                rigidbody.MovePosition(rootMotion);
            }
        }

        /// <summary>
        /// 沿指定方向更新根运动位置，应用方向限制的根运动
        /// </summary>
        /// <param name="direction">期望的移动方向向量</param>
        /// <remarks>
        /// 此方法将根运动位移投影到指定的方向向量上，确保角色沿特定方向移动。
        /// Vector3 是值类型，栈上分配，零GC。
        /// 方向向量会被归一化，确保移动速度与原始根运动位移幅度一致。
        /// </remarks>
        public static void UpdateDirectRootPosition(Rigidbody rigidbody, Vector3 rootMovePosition, Vector3 direction)
        {
            // 使用sqrMagnitude比较代替 != Vector3.zero
            if (rootMovePosition.sqrMagnitude > Constants.VectorNearZeroThreshold)
            {
                Vector3 rootMotion = rigidbody.position + rootMovePosition.magnitude * direction.normalized;
                rigidbody.MovePosition(rootMotion);
            }
        }

        /// <summary>
        /// [优化] 方向缓动工具 — 将 currentDirection 沿 Slerp 平滑转向 targetDirection
        /// 提取自 Sprint/Start_Sprint/Dodge 中的重复代码
        /// </summary>
        /// <param name="currentDirection">当前方向（会被更新）</param>
        /// <param name="targetDirection">目标方向（来自输入）</param>
        /// <param name="rotateSpeed">旋转速度（弧度/秒）</param>
        /// <returns>缓动后的新方向</returns>
        public static Vector3 SmoothRotateDirection(Vector3 currentDirection, Vector3 targetDirection, float rotateSpeed)
        {
            // 使用 sqrMagnitude 阈值比较代替 == Vector3.zero（消除逐分量精度比较的一致性隐患）
            if (currentDirection.sqrMagnitude <= Constants.VectorNearZeroThreshold ||
                targetDirection.sqrMagnitude <= Constants.VectorNearZeroThreshold)
                return currentDirection;

            Quaternion rotation = Quaternion.LookRotation(currentDirection);
            rotation = Quaternion.Slerp(rotation, Quaternion.LookRotation(targetDirection), rotateSpeed * Time.deltaTime);
            return rotation * Vector3.forward;
        }

        /// <summary>
        /// 平滑更新角色的旋转方向，朝向指定的面朝方向
        /// </summary>
        /// <param name="faceDir">目标面朝方向向量</param>
        /// <param name="speed">旋转速度（弧度/秒）</param>
        /// <remarks>
        /// 使用球面线性插值（Slerp）实现平滑旋转，避免突然的方向变化。
        /// 旋转速度参数控制插值速率，值越大旋转越快。
        /// 此方法会更新 variable.FaceDirection 和 transform.rotation 以确保状态同步。
        /// </remarks>
        public static void UpdateRotation(
            Transform transform,
            CharacterVariable variable,
            Vector3 faceDir,
            float speed)
        {
            // 使用sqrMagnitude比较代替 == Vector3.zero
            if (faceDir.sqrMagnitude < Constants.VectorNearZeroThreshold)
            {
                return;
            }

            variable.RotateSpeed = speed;
            variable.FaceDirection = Quaternion.Slerp(
                variable.FaceDirection,
                Quaternion.LookRotation(faceDir),
                speed * Time.fixedDeltaTime
            );
            transform.rotation = variable.FaceDirection;
        }
    }
}
