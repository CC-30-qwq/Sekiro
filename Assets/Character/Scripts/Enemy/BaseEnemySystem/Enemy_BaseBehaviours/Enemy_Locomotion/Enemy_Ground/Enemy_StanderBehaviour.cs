using Sekiro.BehaviourMachine;
using UnityEngine;

namespace Sekiro.Enemy.Behaviours
{
    public class Enemy_StanderBehaviour : EnemyBaseBehaviour
    {
        float _standerTime;
        EnemyMethods.AIMoveDirection _moveDirection;

        public Enemy_StanderBehaviour(EnemyBehaviourMachine machine) : base(machine) { }

        public override bool CanChangeCondition()
        {
            return Timer > _standerTime && Machine.CurrentBehaviour != Machine.NextBehaviour;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Methods.FadeAnimation("Walk");
            _standerTime = Random.Range(2f, 5f);
            _moveDirection = (EnemyMethods.AIMoveDirection)Random.Range(0, 4);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            Methods.CalculateTargetVector2(_moveDirection);

            Methods.UpdateLocomotionParameters();
            Animator.SetFloat("Y", Variable.AnimatorParameterVector2.y);
            Animator.SetFloat("X", Variable.AnimatorParameterVector2.x);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.FindPath(Variable.Target);
            Methods.UpdateModifyRootPosition();
            Methods.UpdateRotation(Variable.PathDirection, CharacterConfig.NormalRotateSpeed);
        }
    }
}
