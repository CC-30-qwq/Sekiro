using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 战斗工具类 — 提供战斗相关的通用方法
    /// </summary>
    public static class CombatUtility
    {
        /// <summary>
        /// 获取指定对象最近一次受击方向
        /// </summary>
        /// <param name="referenceTransform">角色 Transform（用于将世界方向转换到局部坐标系）</param>
        /// <returns>受击方向向量，无受击记录时返回 Vector3.zero</returns>
        public static Vector2 GetHitDirection(Vector3 worldDirection, Transform referenceTransform)
        {
            float dotX = Vector3.Dot(worldDirection, referenceTransform.right);
            float dotZ = Vector3.Dot(worldDirection, referenceTransform.forward);
            float sqrMag = dotX * dotX + dotZ * dotZ;

            Vector2 hitDirection = new Vector2(dotX, dotZ);

            if (sqrMag > Constants.VectorNearZeroThreshold)
            {
                float invMagnitude = 1f / Mathf.Sqrt(sqrMag);
                hitDirection = new Vector2(dotX * invMagnitude, dotZ * invMagnitude);
            }

            float y = hitDirection.y;
            float x = hitDirection.x;

            Vector2 modifyHitDirection = y > 0.5f ? Vector2.up
                : y < -0.5f ? Vector2.down
                : x < 0 ? Vector2.left
                : x > 0 ? Vector2.right
                : Vector2.zero;


            return -modifyHitDirection;
        }

        /// <summary>
        /// 应用击退效果 — 读取 CharacterConfigSO 中的击退参数
        /// </summary>
        /// <param name="rigidbody">目标 Rigidbody</param>
        /// <param name="info">伤害信息（含力大小和方向）</param>
        /// <param name="config">角色配置（读取击退倍率和模式）</param>
        public static void ApplyKnockback(Rigidbody rigidbody, DamageInfo info, CharacterConfigSO config)
        {
            if (rigidbody == null) return;
            if (info.Force <= 0f || info.Direction == Vector3.zero) return;
            if (config == null) return;

            rigidbody.AddForce(
                info.Direction.normalized * info.Force * config.KnockbackForceMultiplier,
                config.KnockbackForceMode
            );
        }
    }
}
