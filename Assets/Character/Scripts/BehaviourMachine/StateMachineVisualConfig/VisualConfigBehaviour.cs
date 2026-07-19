using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 可视化状态转换配置行为组件 - 挂载到 PlayerBehaviourMachine 上
    /// 提供以下功能：
    /// 1. 引用 StateTransitionConfigSO 资产
    /// 2. 将 SO 中的配置转换为运行时规则（支持旧版平铺规则和新版时间线轨道系统）
    /// 3. 提供接口让 GraphView 编辑器查找和修改配置
    /// </summary>
    [RequireComponent(typeof(PlayerBehaviourMachine))]
    public class VisualConfigBehaviour : MonoBehaviour
    {
        [Header("状态转换可视化配置")]
        [Tooltip("引用 ScriptableObject 配置资产，可在 GraphView 编辑器中编辑")]
        [SerializeField] private StateTransitionConfigSO _config;

        /// <summary>
        /// 引用的 ScriptableObject 配置资产
        /// </summary>
        public StateTransitionConfigSO Config
        {
            get => _config;
            set => _config = value;
        }

        /// <summary>
        /// 配置版本计数器，每次配置变化时递增。
        /// PlayerBaseBehaviour 缓存此值检测是否需要重新获取规则。
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// [时间线] 缓存已排序的轨道数组，避免每帧排序
        /// </summary>
        private TransitionTrack[] _cachedSortedTracks;
        private RuleData[] _cachedGlobalRules;
        private PlayerBehaviour _cachedState;
        private int _cachedConfigVersion = -1;

        private PlayerBehaviourMachine _machine;

        /// <summary>
        /// 使配置缓存版本失效（在编辑器运行时编辑 SO 后调用）
        /// </summary>
        public void InvalidateVersion()
        {
            Version++;
        }

        private void Awake()
        {
            _machine = GetComponent<PlayerBehaviourMachine>();
        }

        /// <summary>
        /// 获取指定状态的时间线轨道规则
        /// 返回基于时间线轨道系统的运行时委托数组
        /// </summary>
        public (Func<bool> Condition, Action Action)[] GetTimelineRulesForState(PlayerBaseBehaviour behaviour)
        {
            if (_config == null)
                return null;

            var stateEnum = behaviour.StateBehaviour;
            if (stateEnum == null)
                return null;

            var state = stateEnum.Value;
            var machine = _machine;

            // 获取轨道数据（已排序）
            var (tracks, globalRules) = _config.GetTimelineDataForState(state);

            if ((tracks == null || tracks.Length == 0) && (globalRules == null || globalRules.Length == 0))
                return null;

            return ConditionEvaluator.CreateTrackDelegates(
                tracks,
                globalRules,
                behaviour,
                (targetState) => machine.TryChangeState(targetState)
            );
        }

        /// <summary>
        /// 获取状态转换规则（仅支持时间线轨道系统）
        /// 旧版平铺规则系统已被移除，仅保留时间线轨道系统。
        /// </summary>
        public (Func<bool> Condition, Action Action)[] GetRulesForState(PlayerBaseBehaviour behaviour)
        {
            if (_config == null)
                return null;

            return GetTimelineRulesForState(behaviour);
        }

        /// <summary>
        /// 判断某个行为类是否已在 SO 中配置了可视化规则（时间线系统）
        /// </summary>
        public bool HasVisualConfig(PlayerBaseBehaviour behaviour)
        {
            if (_config == null) return false;
            var stateEnum = behaviour.StateBehaviour;
            return stateEnum != null && _config.HasConfigForState(stateEnum.Value);
        }

        /// <summary>
        /// 判断某个状态是否使用了时间线轨道配置
        /// </summary>
        public bool HasTimelineConfig(PlayerBaseBehaviour behaviour)
        {
            if (_config == null) return false;
            var stateEnum = behaviour.StateBehaviour;
            return stateEnum != null && _config.HasTrackConfigForState(stateEnum.Value);
        }
    }
}

