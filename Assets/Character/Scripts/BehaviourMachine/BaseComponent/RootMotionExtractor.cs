using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 根运动提取组件 — 从 Animator 提取位移并应用速度缩放/插值。
    /// 从 BehaviourMachine.OnAnimatorMove 抽离，独立管理动画处理逻辑。
    /// </summary>
    public class RootMotionExtractor : MonoBehaviour
    {
        [field: SerializeField] public Animator Animator { get; set; }
        [field: SerializeField] public CharacterVariable Variable { get; set; }
        [field: SerializeField] public CharacterConfigSO Config { get; set; }

        private void Awake()
        {
            if (Animator == null)
                Animator = GetComponent<Animator>();
        }

        public void ApplyRootMotion()
        {
            float sqrMag = Animator.deltaPosition.sqrMagnitude;
            bool hasConfig = Config != null;
            float speed = hasConfig ? Config.ModifyMoveSpeed : 1f;
            float slerp = hasConfig ? Config.MoveSlerp : 1f;
            Vector3 rootMotion = sqrMag > Constants.VectorNearZeroThreshold
                ? Animator.deltaPosition * speed
                : Vector3.zero;
            Variable.RootMovePosition = Vector3.Slerp(Variable.RootMovePosition, rootMotion, slerp * Time.deltaTime);
        }
    }
}
