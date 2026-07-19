using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 敌人 Gizmos 调试绘制器 - 从 EnemyBehaviourMachine 分离的独立组件
    /// 负责在编辑器中绘制敌人的调试信息（地面检测、目标方向、攻击范围等）
    /// </summary>
    [RequireComponent(typeof(EnemyBehaviourMachine))]
    public class EnemyGizmosDrawer : MonoBehaviour
    {
        private EnemyBehaviourMachine _machine;
        private EnemyVariable _variable;
        private EnemySetting _setting;

        private void Awake()
        {
            _machine = GetComponent<EnemyBehaviourMachine>();
        }

        private void OnDrawGizmosSelected()
        {
            if (_machine == null)
                _machine = GetComponent<EnemyBehaviourMachine>();

            if (!Application.isPlaying) return;

            _variable = _machine.Variable;
            _setting = _machine.Setting;
            if (_variable == null || _setting == null) return;

            var collider = _setting.ColliderUtility?.CapsuleCollider;
            if (collider == null) return;

            DrawGroundCheck(collider);
            DrawSphereCheck();
            DrawTargetLine();
            DrawAttackRange();
        }

        private void DrawGroundCheck(CapsuleCollider collider)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_variable.GroundPoint, 0.1f);

            Gizmos.color = _variable.IsGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(collider.bounds.center, Vector3.down * _setting.ColliderUtility.FloatRayDistance);
        }

        private void DrawSphereCheck()
        {
            Gizmos.color = _variable.IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(
                transform.position + _setting.GroundCheck.SphereCheckOffset,
                _setting.GroundCheck.SphereCheckRadius);
        }

        private void DrawTargetLine()
        {
            if (_variable.Target == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _variable.Target.position);
        }

        private void DrawAttackRange()
        {
            var enemyConfig = _machine.EnemyConfig;
            if (enemyConfig == null) return;

            Gizmos.color = Color.red;
            float farRange = enemyConfig.FarRange;
            for (int i = 0; i < (int)farRange; i++)
            {
                Gizmos.DrawLine(
                    transform.position + transform.forward * i - transform.right * 0.5f,
                    transform.position + transform.forward * i + transform.right * 0.5f);
            }
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * farRange);
        }
    }
}
