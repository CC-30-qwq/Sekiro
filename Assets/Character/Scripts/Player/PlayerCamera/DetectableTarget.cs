using UnityEngine;

namespace Sekiro.Combat
{
    /// <summary>
    /// 检测结果 — 经过检测管道筛选后的有效目标
    /// </summary>
    public readonly struct DetectableTarget
    {
        public Transform Root { get; }
        public Transform HitPoint { get; }
        public float Score { get; }

        public DetectableTarget(Transform root, Transform hitPoint, float score)
        {
            Root = root;
            HitPoint = hitPoint;
            Score = score;
        }
    }
}
