using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 动画工具类 - 从 BehaviourMethods 中提取的独立职责
    /// 提供动画淡入淡出、动画参数更新等功能
    /// </summary>
    public static class AnimationUtility
    {
        #region Animation Methods

        /// <summary>
        /// 动画淡入淡出（基于预计算Hash - GC优化版本）
        /// </summary>
        /// <param name="animator">动画器组件</param>
        /// <param name="animName">动画名称，用于从字典获取哈希值和时间</param>
        /// <param name="animHash">当前角色类型的动画哈希字典</param>
        public static void FadeAnimation(Animator animator, string animName, System.Collections.Generic.Dictionary<int, float> animHash)
        {
            int stateHash = Animator.StringToHash(animName);
            float time = animHash.TryGetValue(stateHash, out float fadeTime) ? fadeTime : 0.0f;
            animator.CrossFade(stateHash, time, 1, 0);
        }

        /// <summary>
        /// 动画淡入淡出（使用自定义时间）
        /// </summary>
        /// <param name="animator">动画器组件</param>
        /// <param name="stateHash">动画状态哈希值</param>
        /// <param name="time">淡入淡出时间</param>
        public static void FadeAnimation(Animator animator, int stateHash, float time)
        {
            animator.CrossFade(stateHash, time, 1, 0);
        }

        #endregion

        #region Animation Parameter Methods

		/// <summary>
		/// 更新移动参数 - 包含动画参数缓动逻辑
		/// </summary>
		/// <param name="variable">角色变量</param>
		/// <param name="lerpSpeed">动画参数缓动速度，从 CharacterConfigSO.AnimParameterLerp 传入</param>
		/// <remarks>
		/// 旧版带 CharacterSetting 参数的重载已移除（曾使用硬编码 5f 默认值），
		/// 调用方请从 CharacterConfigSO.AnimParameterLerp 获取 lerpSpeed 传入
		/// </remarks>
		public static void UpdateLocomotionParameters(CharacterVariable variable, float lerpSpeed)
		{
			float y = variable.AnimInputValue.y;
			float x = variable.AnimInputValue.x;

			variable.TargetVector = y > 0.8f ? Vector2.up
				: y < -0.8f ? Vector2.down
				: x < 0 ? Vector2.left
				: x > 0 ? Vector2.right
				: Vector2.zero;

			var target = Vector2.Lerp(
				variable.AnimatorParameterVector2,
				variable.TargetVector,
				lerpSpeed * Time.deltaTime
			);

			variable.AnimatorParameterVector2 = target.sqrMagnitude < Constants.VectorNearZeroThreshold
				? Vector2.zero
				: target;
		}

        /// <summary>
        /// 计算目标二维动画输入值 - 将世界空间方向向量转换为角色局部空间的标准化 Vector2
        /// 通用版本供 Player/Enemy 共享使用，避免代码重复
        /// </summary>
        public static Vector2 CalculateAnimInputValue(Vector3 worldDirection, Transform referenceTransform)
        {
            float dotX = Vector3.Dot(worldDirection, referenceTransform.right);
            float dotZ = Vector3.Dot(worldDirection, referenceTransform.forward);
            float sqrMag = dotX * dotX + dotZ * dotZ;

            if (sqrMag > Constants.VectorNearZeroThreshold)
            {
                float invMagnitude = 1f / Mathf.Sqrt(sqrMag);
                return new Vector2(dotX * invMagnitude, dotZ * invMagnitude);
            }

            return Vector2.zero;
        }

        #endregion
    }
}
