using System;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// Start air behaviour base class - 开始空中行为基类
    /// </summary>
    public class AirBehaviour : PlayerBaseBehaviour
    {
        public AirBehaviour(PlayerBehaviourMachine machine) : base(machine)
        {
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Methods.UpdateVerticalPosition(Vector3.up);
        }
    }
}