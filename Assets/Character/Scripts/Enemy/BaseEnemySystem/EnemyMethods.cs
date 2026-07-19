using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 敌人行为方法工具类
    /// </summary>
    public class EnemyMethods : BehaviourMethods<EnemySetting, EnemyVariable>
    {
        public EnemyVariable EnemyVariable => base.Variable as EnemyVariable;
        public EnemySetting EnemySetting => base.Setting as EnemySetting;

        #region Performance Optimization

        private const float _pathUpdateInterval = 0.2f;
        private float _lastPathUpdateTime;

        #endregion

        #region Cached References

        private Transform _cachedTransform;

        #endregion

        public void FindPath(Transform target)
        {
            if (target == null) return;

            float currentTime = Time.time;
            if (currentTime - _lastPathUpdateTime < _pathUpdateInterval)
            {
                return;
            }

            Agent.SetDestination(target.position);
            _lastPathUpdateTime = currentTime;
        }

        /// <summary>
        /// AI 预期移动方向枚举
        /// </summary>
        public enum AIMoveDirection
        {
            Forward = 0,
            Backward = 1,
            Left = 2,
            Right = 3
        }

        /// <summary>
        /// 计算目标二维向量（用于动画混合树）
        /// </summary>
        public void CalculateTargetVector2(AIMoveDirection direction)
        {
            var v = EnemyVariable;
            var cachedTransform = _cachedTransform ??= Transform;

            Vector3 aiExpectDirection = direction switch
            {
                AIMoveDirection.Forward => Vector3.forward,
                AIMoveDirection.Backward => Vector3.back,
                AIMoveDirection.Left => Vector3.left,
                AIMoveDirection.Right => Vector3.right,
                _ => Vector3.zero,
            };

            v.AnimInputValue = CalculateAnimInputValue(aiExpectDirection, cachedTransform);
        }

    }
}
