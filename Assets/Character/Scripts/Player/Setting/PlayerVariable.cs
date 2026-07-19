using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 玩家变量数据类，继承自 CharacterVariable
    /// </summary>
    [System.Serializable]
    public class PlayerVariable : CharacterVariable
    {
        [Header("Input")]
        public Vector3 InputDirection;
        public Vector3 BufferedDirection;
    }
}
