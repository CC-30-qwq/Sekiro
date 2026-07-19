using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 移动驱动组件 — 将根运动模式、旋转、输入缓冲集中到一处调度，
    /// 替代各行为子类在 OnFixedUpdate/OnUpdate 中散落的 60+ 处透视方法调用。
    ///
    /// 使用方式：行为在 OnEnter 中设置模式枚举和参数，
    /// PlayerBaseBehaviour 在 OnFixedUpdate/OnUpdate 中统一调用 Apply()。
    /// </summary>
    public class LocomotionDriver
    {
        public enum RootMotionMode { None, Modify, Origin, Direct }
        public enum RotationSource { None, InputDirection, BufferedDirection, FixedDirection }
        public enum BufferInputMode { None, Persist, Forward }

        public RootMotionMode ActiveRootMotion;
        public RotationSource ActiveRotation;
        public float RotationSpeed;
        public Vector3 FixedDirection;
        public BufferInputMode BufferInput;

        public void Reset()
        {
            ActiveRootMotion = RootMotionMode.None;
            ActiveRotation = RotationSource.None;
            RotationSpeed = 0f;
            FixedDirection = Vector3.zero;
            BufferInput = BufferInputMode.None;
        }

        public void ApplyFixedUpdate(
            Rigidbody rigidbody, Transform transform,
            CharacterVariable variable, CharacterConfigSO config,
            PlayerMethods methods)
        {
            switch (ActiveRootMotion)
            {
                case RootMotionMode.Modify:
                    methods.UpdateModifyRootPosition();
                    break;
                case RootMotionMode.Origin:
                    methods.UpdateOriginRootPosition();
                    break;
                case RootMotionMode.Direct:
                    methods.UpdateDirectRootPosition(FixedDirection);
                    break;
            }

            Vector3 rotationDir = ActiveRotation switch
            {
                RotationSource.InputDirection => variable is PlayerVariable pv ? pv.InputDirection : variable.AnimatorParameterVector2,
                RotationSource.BufferedDirection => variable is PlayerVariable pv ? pv.BufferedDirection : transform.forward,
                RotationSource.FixedDirection => FixedDirection,
                _ => Vector3.zero,
            };

            if (ActiveRotation != RotationSource.None)
                methods.UpdateRotation(rotationDir, RotationSpeed);
        }

        public void ApplyUpdate(PlayerMethods methods, PlayerVariable variable, Transform transform)
        {
            switch (BufferInput)
            {
                case BufferInputMode.Persist:
                    methods.BufferInputDirection();
                    break;
                case BufferInputMode.Forward:
                    methods.BufferInputDirection(transform.forward);
                    break;
            }
        }
    }
}
