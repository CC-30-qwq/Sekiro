using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 玩家行为方法工具类
    /// </summary>
    public class PlayerMethods : BehaviourMethods<PlayerSetting, PlayerVariable>
    {
        public PlayerVariable PlayerVariable => base.Variable as PlayerVariable;
        public PlayerSetting PlayerSetting => base.Setting as PlayerSetting;

        #region Cached References

        private Transform _cachedTransform;
        private Camera _cachedCamera;
        private Vector3 _cachedCameraForward;

        #endregion

        #region Character Config Accessor

        /// <summary>
        /// 角色配置辅助属性 — 从基类 BehaviourMethods 获取
        /// 替代旧的 PlayerSetting.CharacterStatus 配置字段
        /// </summary>
        private new CharacterConfigSO CharacterConfig => base.CharacterConfig;

        #endregion

        #region Input Methods

        /// <summary>
        /// 根据相机方向更新输入方向
        /// </summary>
        public void UpdateInputDirection()
        {
            // [优化] 使用显式 null 检查替代 ??=，防止 camera 返回 null 时无法正确缓存
            if (_cachedCamera == null)
                _cachedCamera = PlayerSetting.Camera;
            var camera = _cachedCamera;
            if (camera != null)
            {
                var camForward = camera.transform.forward;
                // 重用_cachedCameraForward代替临时向量
                _cachedCameraForward.x = camForward.x;
                _cachedCameraForward.z = camForward.z;
                _cachedCameraForward.y = 0;
            }
            else
            {
                _cachedCameraForward = Vector3.forward;
            }

            // 计算输入方向
            var moveInput = Inputs.MoveInput;
            var inputVec = (_cachedCameraForward * moveInput.y) + (Vector3.Cross(Vector3.up, _cachedCameraForward) * moveInput.x);
            PlayerVariable.InputDirection = new Vector3(inputVec.x, 0, inputVec.z);
        }

        /// <summary>
        /// 计算目标二维向量
        /// </summary>
        public void CalculateTargetVector2()
        {
            var v = PlayerVariable;
            var cachedTransform = _cachedTransform ??= Transform;
            v.AnimInputValue = CalculateAnimInputValue(v.InputDirection, cachedTransform);
        }

        /// <summary>
        /// 缓冲输入方向
        /// </summary>
        public void BufferInputDirection()
        {
            // 使用sqrMagnitude比较代替 != Vector3.zero
            PlayerVariable.BufferedDirection = PlayerVariable.InputDirection.sqrMagnitude > Constants.VectorNearZeroThreshold
                ? PlayerVariable.InputDirection
                : PlayerVariable.BufferedDirection;
        }
        public void BufferInputDirection(Vector3 defaultDirection)
        {
            PlayerVariable.BufferedDirection = defaultDirection;
        }

        #endregion

        #region Movement Methods

        /// <summary>
        /// 更新垂直位置
        /// </summary>
        public void UpdateVerticalPosition(Vector3 upDir)
        {
            Rigidbody.MovePosition(Rigidbody.position + Variable.VerticalSpeed * upDir * Time.fixedDeltaTime);
        }

        #endregion

        #region Force Methods

        /// <summary>
        /// 添加水平力
        /// </summary>
        public void AddHorizontalForce(float force, Vector3 direction, bool accelerate)
        {
            Rigidbody.AddForce(force * direction, accelerate ? ForceMode.Acceleration : ForceMode.VelocityChange);
        }

        /// <summary>
        /// 添加垂直力
        /// </summary>
        public void AddVerticalForce(float height)
        {
            Variable.VerticalSpeed = Mathf.Sqrt(height * -2f * Physics.gravity.y);
        }

        /// <summary>
        /// 添加重力/阻力 — 直接从 CharacterConfigSO 读取配置参数
        /// </summary>
        public void AddGravityForce(bool add)
        {
            if (add)
            {
                float gravityMult = CharacterConfig != null ? CharacterConfig.GravityMultiplier : 1f;
                float airDrag = CharacterConfig != null ? CharacterConfig.AirDragSpeed : 5f;

                Variable.VerticalSpeed += Physics.gravity.y * gravityMult * Time.deltaTime;
                Variable.HorizontalSpeed = Variable.HorizontalSpeed > 0
                    ? Variable.HorizontalSpeed - airDrag * Time.deltaTime
                    : 0;
            }
            else
            {
                Variable.VerticalSpeed = 0;
                Variable.HorizontalSpeed = 0;
            }
        }

        #endregion
    }
}
