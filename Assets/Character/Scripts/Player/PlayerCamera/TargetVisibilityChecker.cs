using UnityEngine;

namespace Sekiro.Combat
{
    /// <summary>
    /// 可见性检查 — 视野锥角 + 视锥 + 遮挡
    /// </summary>
    public class TargetVisibilityChecker
    {
        private readonly Camera _camera;
        private readonly float _maxViewAngle;
        private readonly LayerMask _barrierLayer;
        private readonly Plane[] _frustumPlanes = new Plane[6];

        public int DiagOutOfViewAngle { get; set; }
        public int DiagOutOfFrustum { get; set; }
        public int DiagOccluded { get; set; }

        public TargetVisibilityChecker(Camera camera, float maxViewAngle, LayerMask barrierLayer)
        {
            _camera = camera;
            _maxViewAngle = maxViewAngle;
            _barrierLayer = barrierLayer;
        }

        /// <summary>综合检查：视野锥角 → 视锥 → 遮挡</summary>
        public bool IsVisible(Transform detectPoint)
        {
            if (!IsWithinViewAngle(detectPoint)) { DiagOutOfViewAngle++; return false; }
            if (!IsInViewFrustum(detectPoint)) { DiagOutOfFrustum++; return false; }
            if (!HasLineOfSight(detectPoint)) { DiagOccluded++; return false; }
            return true;
        }

        /// <summary>
        /// 检查目标是否在摄像机前方的视野锥角范围内
        /// 计算摄像机前方向量与目标方向向量的 3D 夹角
        /// </summary>
        private bool IsWithinViewAngle(Transform target)
        {
            Vector3 toTarget = target.position - _camera.transform.position;
            return Vector3.Angle(_camera.transform.forward, toTarget) <= _maxViewAngle;
        }

        /// <summary>
        /// 检查目标是否在摄像机视锥体内
        /// 优先使用 Renderer.bounds 进行精确 AABB 测试，否则使用 ViewportPoint 兜底
        /// </summary>
        private bool IsInViewFrustum(Transform target)
        {
            if (target.TryGetComponent<Renderer>(out var renderer))
            {
                GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);
                return GeometryUtility.TestPlanesAABB(_frustumPlanes, renderer.bounds);
            }

            Vector3 vp = _camera.WorldToViewportPoint(target.position);
            return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f && vp.z > 0f;
        }

        private bool HasLineOfSight(Transform target)
        {
            return !Physics.Linecast(_camera.transform.position, target.position, _barrierLayer);
        }

        public void ResetDiagnostics()
        {
            DiagOutOfViewAngle = 0;
            DiagOutOfFrustum = 0;
            DiagOccluded = 0;
        }
    }
}
