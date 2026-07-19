using UnityEngine;

namespace Sekiro.Combat
{
    /// <summary>
    /// 物理扫描 — OverlapSphere 收集候选
    /// </summary>
    public class TargetDetector
    {
        private readonly Transform _origin;
        private readonly float _radius;
        private readonly LayerMask _targetLayer;
        private Collider[] _overlapResults;

        public TargetDetector(Transform origin, float radius, LayerMask targetLayer, int bufferSize = 32)
        {
            _origin = origin;
            _radius = radius;
            _targetLayer = targetLayer;
            _overlapResults = new Collider[bufferSize];
        }

        /// <returns>候选数组有效长度</returns>
        public int CollectCandidates()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                _origin.position, _radius, _overlapResults, _targetLayer);

            if (hitCount >= _overlapResults.Length)
            {
                _overlapResults = new Collider[_overlapResults.Length * 2];
                hitCount = Physics.OverlapSphereNonAlloc(
                    _origin.position, _radius, _overlapResults, _targetLayer);
            }

            return hitCount;
        }

        public Collider GetCandidate(int index) => _overlapResults[index];
    }
}
