using System.Collections.Generic;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 状态转换配置资产 - 存储整个状态机的可视化配置
    /// 包括所有状态节点的位置和它们的转换规则（支持时间线轨道系统）
    /// </summary>
    [CreateAssetMenu(menuName = "Sekiro/State Machine/Transition Config", fileName = "PlayerTransitionConfig")]
    public class StateTransitionConfigSO : ScriptableObject
    {
        /// <summary>所有状态的转换规则数据</summary>
        public List<StateNodeData> nodeDataList = new List<StateNodeData>();

        /// <summary>
        /// 获取指定状态的规则数据，如果不存在则创建
        /// </summary>
        public StateNodeData GetOrCreateNodeData(PlayerBehaviour state)
        {
            var existing = nodeDataList.Find(n => n.state == state);
            if (existing != null)
                return existing;

            var newNode = new StateNodeData
            {
                state = state,
                position = Vector2.zero,
                tracks = new List<TransitionTrack>(),
                globalRules = new RuleData[0]
            };
            nodeDataList.Add(newNode);
            return newNode;
        }

        /// <summary>
        /// 获取指定状态的规则数据，不存在返回 null
        /// </summary>
        public StateNodeData GetNodeData(PlayerBehaviour state)
        {
            return nodeDataList.Find(n => n.state == state);
        }

        /// <summary>
        /// 获取指定状态的轨道数据（时间线轨道系统）
        /// </summary>
        public TransitionTrack[] GetTracksForState(PlayerBehaviour state)
        {
            var node = GetNodeData(state);
            return node?.tracks?.ToArray() ?? System.Array.Empty<TransitionTrack>();
        }

        /// <summary>
        /// [新] 获取指定状态的全局规则
        /// </summary>
        public RuleData[] GetGlobalRulesForState(PlayerBehaviour state)
        {
            var node = GetNodeData(state);
            return node?.globalRules ?? System.Array.Empty<RuleData>();
        }

        /// <summary>
        /// [新] 获取所有启用的轨道（按优先级排序）
        /// 同时返回该状态的全局规则
        /// </summary>
        public (TransitionTrack[] tracks, RuleData[] globalRules) GetTimelineDataForState(PlayerBehaviour state)
        {
            var node = GetNodeData(state);
            if (node == null)
                return (System.Array.Empty<TransitionTrack>(), System.Array.Empty<RuleData>());

            return (node.GetTracksSortedByPriority(), node.globalRules ?? System.Array.Empty<RuleData>());
        }

        /// <summary>
        /// [新] 检查某个状态是否有已配置的轨道规则
        /// </summary>
        public bool HasTrackConfigForState(PlayerBehaviour state)
        {
            var node = GetNodeData(state);
            if (node == null) return false;
            if (node.tracks != null && node.tracks.Count > 0 && node.tracks.Exists(t => t.isActive && t.clips.Count > 0))
                return true;
            if (node.globalRules != null && node.globalRules.Length > 0)
                return true;
            return false;
        }

        /// <summary>
        /// 检查某个状态是否有已配置的转换规则（时间线轨道系统）
        /// </summary>
        public bool HasConfigForState(PlayerBehaviour state)
        {
            var node = GetNodeData(state);
            if (node == null) return false;
            if (node.globalRules != null && node.globalRules.Length > 0) return true;
            if (node.tracks != null && node.tracks.Count > 0)
            {
                foreach (var track in node.tracks)
                {
                    if (track.isActive && track.clips.Count > 0)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 清除所有配置数据
        /// </summary>
        public void Clear()
        {
            nodeDataList.Clear();
        }
    }
}

