using UnityEngine;
using Sekiro.BehaviourMachine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 玩家 Gizmos 调试绘制器 - 从 PlayerBehaviourMachine 分离的独立组件
    /// 负责在编辑器中绘制玩家角色的调试信息（方向、地面检测、动画参数等）
    /// </summary>
    [RequireComponent(typeof(PlayerBehaviourMachine))]
    public class PlayerGizmosDrawer : MonoBehaviour
    {
        #region Gizmo Colors

        private static class GizmoColors
        {
            public static readonly Color CharacterDirection = Color.blue;
            public static readonly Color InputDirection = Color.green;
            public static readonly Color BufferedDirection = Color.yellow;
            public static readonly Color GroundCheckHit = new Color(0, 1, 0, 0.6f);
            public static readonly Color GroundCheckMiss = new Color(1, 0, 0, 0.6f);
            public static readonly Color AnimParameter = Color.magenta;
            public static readonly Color GroundPoint = Color.cyan;
        }

        #endregion

        private PlayerBehaviourMachine _machine;

        private void Awake()
        {
            _machine = GetComponent<PlayerBehaviourMachine>();
        }

        /// <summary>
        /// 绘制箭头射线（主轴 + 左右箭头翼）
        /// </summary>
        /// <param name="origin">起点</param>
        /// <param name="direction">方向（已归一化）</param>
        /// <param name="length">箭头长度</param>
        private static void DrawArrowRay(Vector3 origin, Vector3 direction, float length)
        {
            Gizmos.DrawRay(origin, direction * length);
            var arrowEnd = origin + direction * (length - length * 0.2f);
            Gizmos.DrawRay(arrowEnd, Quaternion.Euler(0, 30, 0) * direction * length * 0.15f);
            Gizmos.DrawRay(arrowEnd, Quaternion.Euler(0, -30, 0) * direction * length * 0.15f);
        }

        private void OnDrawGizmosSelected()
        {
            if (_machine == null)
                _machine = GetComponent<PlayerBehaviourMachine>();

            // 运行时才需要绘制，且需要Variable和Setting已初始化
            if (!Application.isPlaying) return;

            var variable = _machine.Variable;
            var setting = _machine.Setting;
            if (variable == null || setting == null) return;

            var cachedTransform = transform;
            var position = cachedTransform.position;

            // ---------------------------------------------------------------
            // 1. 角色当前方向 (Character Forward) - 蓝色
            // ---------------------------------------------------------------
            var characterForward = cachedTransform.forward;
            Gizmos.color = GizmoColors.CharacterDirection;
            DrawArrowRay(position, characterForward, 0.5f);

            // ---------------------------------------------------------------
            // 2. 输入方向 (Input Direction) - 绿色
            // ---------------------------------------------------------------
            var inputDir = variable.InputDirection;
            if (inputDir.sqrMagnitude > Constants.VectorNearZeroThreshold)
            {
                inputDir = inputDir.normalized;
                Gizmos.color = GizmoColors.InputDirection;
                DrawArrowRay(position + Vector3.up * 2, inputDir, 0.5f);
            }

            // ---------------------------------------------------------------
            // 3. 输入缓冲方向 (Buffered Direction) - 黄色
            // ---------------------------------------------------------------
            var bufferedDir = variable.BufferedDirection;
            if (bufferedDir.sqrMagnitude > Constants.VectorNearZeroThreshold)
            {
                bufferedDir = bufferedDir.normalized;
                Gizmos.color = GizmoColors.BufferedDirection;
                DrawArrowRay(position + Vector3.up * 2, bufferedDir, 0.5f);
            }

            // ---------------------------------------------------------------
            // 4. 地面检测射线 (Ground Check Ray)
            // ---------------------------------------------------------------
            var colliderUtility = setting.ColliderUtility;
            if (colliderUtility != null && colliderUtility.CapsuleCollider != null)
            {
                var rayOrigin = colliderUtility.CapsuleCollider.bounds.center;
                var groundCheck = setting.GroundCheck;
                if (groundCheck == null) return;

                float rayDist = colliderUtility.FloatRayDistance;
                int rayCount = setting.ColliderUtility.CircleRayCount;
                float radius = groundCheck.SphereCheckRadius;

                Gizmos.color = variable.IsGrounded ? GizmoColors.GroundCheckHit : GizmoColors.GroundCheckMiss;
                for (int i = 0; i < rayCount; i++)
                {
                    float angle = 360f / rayCount * i;
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                    Vector3 ray = rayOrigin + direction * radius;
                    Gizmos.DrawRay(ray, Vector3.down * rayDist);
                }
                Gizmos.DrawRay(rayOrigin, Vector3.down * variable.GroundRayHit.distance);

                Gizmos.color = GizmoColors.GroundPoint;
                Gizmos.DrawWireSphere(variable.GroundRayHit.point, 0.1f);

                Gizmos.color = Color.white;
                Gizmos.DrawRay(variable.GroundRayHit.point, variable.GroundRayHit.normal * 0.3f);
            }

            // ---------------------------------------------------------------
            // 5. 动画参数方向 (Animator Parameter Direction) - 品红色
            // ---------------------------------------------------------------
            var animParamVec2 = variable.AnimatorParameterVector2;
            if (animParamVec2.sqrMagnitude > Constants.VectorNearZeroThreshold)
            {
                Vector3 animDir = (cachedTransform.forward * animParamVec2.y + cachedTransform.right * animParamVec2.x).normalized;
                float magnitude = animParamVec2.magnitude;
                Gizmos.color = GizmoColors.AnimParameter;
                Gizmos.DrawRay(position, animDir * 0.5f * magnitude);
                Gizmos.DrawRay(position + animDir * 0.4f * magnitude, Quaternion.Euler(0, 30, 0) * animDir * 0.15f * magnitude);
                Gizmos.DrawRay(position + animDir * 0.4f * magnitude, Quaternion.Euler(0, -30, 0) * animDir * 0.15f * magnitude);
            }
        }
    }
}
