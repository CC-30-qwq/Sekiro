using System.Collections.Generic;
using Sekiro.BehaviourTree;
using Sekiro.Enemy.Setting;
using UnityEngine;
using UnityEngine.UI;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 敌人变量数据类，继承自 CharacterVariable
    /// </summary>
    [System.Serializable]
    public class EnemyVariable : CharacterVariable
    {
        [Header("Path")]
        public Vector3 PathDirection;

        [Header("AttackList")]
        public List<EnemyAttackMappingSO.AttackEntry> AvailableAttacks;
    }
}
