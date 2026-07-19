using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 地面检测工具类
    /// </summary>
    [System.Serializable]
    public class GroundCheckUtility
    {
        #region Private Fields

        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Vector3 _sphereCheckOffset;
        [SerializeField] private float _sphereCheckRadius;

        #endregion

        #region Properties

        public LayerMask GroundLayer => _groundLayer;
        public Vector3 SphereCheckOffset => _sphereCheckOffset;
        public float SphereCheckRadius => _sphereCheckRadius;

        #endregion
    }
}
