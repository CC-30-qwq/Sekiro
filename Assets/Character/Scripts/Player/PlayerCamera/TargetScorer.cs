using UnityEngine;

namespace Sekiro.Combat
{
    /// <summary>
    /// 目标评分器 — 对检测通过的目标进行优先级评分
    /// 
    /// 设计思路：
    /// - 分离评分逻辑，使其可测试、可扩展
    /// - 支持插入自定义评分策略（通过 IFactorScorer 接口）
    /// - 分数越低优先级越高（靠近屏幕中心 + 距离近）
    /// </summary>
    public class TargetScorer
    {
        #region Settings

        private readonly Camera _camera;
        private readonly float _screenCenterWeight;
        private readonly float _distanceWeight;

        #endregion

        public TargetScorer(Camera camera, float screenCenterWeight, float distanceWeight)
        {
            _camera = camera;
            _screenCenterWeight = screenCenterWeight;
            _distanceWeight = distanceWeight;
        }

        /// <summary>
        /// 计算目标评分 — 分数越低优先级越高
        /// 
        /// 评分公式：
        ///   score = screenDist * _screenCenterWeight + distance * _distanceWeight - detectable.PriorityBoost
        /// 
        /// 其中：
        ///   screenDist: 目标在视口中的距离中心点的距离（0~~0.707）
        ///   distance: 目标到检测源的距离
        ///   PriorityBoost: IDetectable 提供的优先级偏移
        /// </summary>
        public float CalculateScore(Vector3 sourcePosition, Transform targetPoint, IDetectable detectable)
        {
            Vector3 vp = _camera.WorldToViewportPoint(targetPoint.position);
            float screenDist = new Vector2(vp.x - 0.5f, vp.y - 0.5f).magnitude;
            float distance = Vector3.Distance(sourcePosition, targetPoint.position);

            float rawScore = screenDist * _screenCenterWeight + distance * _distanceWeight;

            // 应用优先级偏移（正数 = 更高优先级 = 分数降低）
            float finalScore = Mathf.Max(0f, rawScore - detectable.PriorityBoost);

            return finalScore;
        }

    }
}
