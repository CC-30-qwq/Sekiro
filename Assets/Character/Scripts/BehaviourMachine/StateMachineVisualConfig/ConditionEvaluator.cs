using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 运行时条件评估器 - 将 ConditionData 转换为可执行的条件判断
    /// 
    /// [重构] 使用字典注册模式替代巨型 switch-case：
    /// - 每个 ConditionType 通过 Register 注册评估函数
    /// - 新增条件类型只需在 RegisterAll 中添加一行注册，不再修改 Evaluate 方法
    /// - 零GC：所有评估函数注册为静态委托，无装箱/拆箱
    /// - 线程安全的静态初始化
    /// 
    /// [重构] 接口化改造（Phase 2）：
    /// - 使用 IConditionDataProvider 接口替代 PlayerBaseBehaviour 具体类型
    /// - 解耦：Enemy 或其他角色类型实现接口后即可复用
    /// - 可测试：可通过 mock 接口实现进行单元测试
    /// </summary>
    public static class ConditionEvaluator
    {
        #region Condition Registry

        /// <summary>
        /// 条件评估函数委托（使用接口解耦）
        /// 公开为 public 以允许外部模块注册自定义条件类型
        /// </summary>
        public delegate bool ConditionEvaluatorFunc(ConditionData condition, IConditionDataProvider dataProvider);

        /// <summary>
        /// 条件注册表 - ConditionType → 评估函数
        /// </summary>
        private static readonly Dictionary<ConditionType, ConditionEvaluatorFunc> _registry =
            new Dictionary<ConditionType, ConditionEvaluatorFunc>();

        /// <summary>
        /// 动作执行注册表 - TransitionActionType → 执行函数
        /// </summary>
        private static readonly Dictionary<TransitionActionType, Action<TransitionActionData, IConditionDataProvider>> _actionRegistry =
            new Dictionary<TransitionActionType, Action<TransitionActionData, IConditionDataProvider>>();

        /// <summary>
        /// 静态构造函数 - 注册所有条件和动作
        /// </summary>
        static ConditionEvaluator()
        {
            RegisterAll();
            RegisterAllActions();
        }

        /// <summary>
        /// [热加载] 清空并重新注册所有条件和动作。
        /// 在 AssemblyReloadEvents.beforeAssemblyReload 中调用，支持运行时新增条件类型。
        /// 如果使用 Mod 或 DLC 加载新条件类型，可调用此方法刷新注册表。
        /// </summary>
        public static void Reload()
        {
            _registry.Clear();
            _actionRegistry.Clear();
            RegisterAll();
            RegisterAllActions();
        }

        /// <summary>
        /// [运行时初始化] 在场景加载前执行 Reload，确保注册表状态一致。
        /// 配合 Unity 域重载（Domain Reload）使用，防止编辑器重载后注册表为空。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            Reload();
        }

        /// <summary>
        /// 注册单个条件类型的评估函数
        /// </summary>
        private static void Register(ConditionType type, ConditionEvaluatorFunc evaluator)
        {
            _registry[type] = evaluator;
        }

        /// <summary>
        /// 注册所有内置条件类型 — 通过 IConditionDataProvider 接口读取数据
        /// </summary>
        private static void RegisterAll()
        {
            // 使用 PeekIns（非破坏性检测），实际消耗延迟到 Action 阶段执行
            // 避免 AND 链中后续条件失败或后续轨道检测时缓冲区已被消耗
            Register(ConditionType.InputPressed, (c, d) => d.PeekIns(c.inputType));
            Register(ConditionType.DefenceHeld, (c, d) => d.DefenceInput);
            Register(ConditionType.SprintHeld, (c, d) => d.SprintInput);
            Register(ConditionType.NotDefenceHeld, (c, d) => !d.DefenceInput);

            Register(ConditionType.IsGrounded, (c, d) => d.IsGrounded);
            Register(ConditionType.NotGrounded, (c, d) => !d.IsGrounded);
            Register(ConditionType.TargetVectorYEquals, (c, d) => Mathf.Approximately(d.TargetVector.y, c.floatValue));
            Register(ConditionType.AnimInputMagnitudeZero, (c, d) => d.AnimInputValue.sqrMagnitude <= Constants.VectorNearZeroThreshold);
            Register(ConditionType.AnimInputMagnitudeNotZero, (c, d) => d.AnimInputValue.sqrMagnitude > Constants.VectorNearZeroThreshold);
            Register(ConditionType.VerticalSpeedCheck, (c, d) => d.VerticalSpeed < 0);
            Register(ConditionType.JumpDirectionZero, (c, d) => d.JumpDirection.sqrMagnitude <= Constants.VectorNearZeroThreshold);
            Register(ConditionType.JumpDirectionNotZero, (c, d) => d.JumpDirection.sqrMagnitude > Constants.VectorNearZeroThreshold);

            // ============ 行为字段查询条件（通过 IConditionDataProvider 接口） ============
            Register(ConditionType.SprintReleaseTimerElapsed, (c, d) => d.SprintReleaseTimerValue > c.floatValue);
            Register(ConditionType.SprintHoldTimerElapsed, (c, d) => d.SprintHoldTimerValue > c.floatValue);
            Register(ConditionType.NotAttacking, (c, d) => !d.IsAttackingValue);
            Register(ConditionType.NotParrying, (c, d) => !d.IsParryingValue);
            Register(ConditionType.AttackStateMatch, (c, d) => d.CurrentAttackStateValue == c.attackState);
            Register(ConditionType.ParryStateMatch, (c, d) => d.CurrentParryStateValue == c.parryState);
            Register(ConditionType.TurnIsLeft, (c, d) => d.TurnIsLeft);
            Register(ConditionType.TurnIsRight, (c, d) => d.TurnIsRight);
        }

        /// <summary>
        /// 注册动作执行函数
        /// </summary>
        private static void RegisterAction(TransitionActionType type, Action<TransitionActionData, IConditionDataProvider> executor)
        {
            _actionRegistry[type] = executor;
        }

        private static void RegisterAllActions()
        {
            RegisterAction(TransitionActionType.SetComboReference, (a, d) =>
            {
                // 直接引用 ComboConfigSO 资产，不依赖 ComboDatabase 列表顺序
                if (a.comboReference != null)
                {
                    d.CurrentCombo = a.comboReference;
                }
            });
            RegisterAction(TransitionActionType.SetJumpForce, (a, d) => d.HorizontalSpeed = a.floatValue);
        }

        /// <summary>
        /// 允许外部注册自定义条件类型（扩展点）
        /// 使用 ConditionEvaluatorFunc 公共委托类型以保持一致
        /// </summary>
        public static void RegisterCustom(ConditionType type, ConditionEvaluatorFunc evaluator)
        {
            _registry[type] = evaluator;
        }

        #endregion

        #region Public API (PlayerBaseBehaviour overloads — interface-adapted)

        public static (Func<bool> Condition, Action Action) CreateRuleDelegate(
            RuleData rule,
            PlayerBaseBehaviour behaviour,
            Action<PlayerBehaviour> changeStateAction)
        {
            IConditionDataProvider dataProvider = behaviour;
            return (
                () => EvaluateAll(rule.conditions, dataProvider),
                () =>
                {
                    // PeekIns 已在条件评估阶段非破坏性检测通过，此时消耗输入缓冲区
                    if (HasInputPressedCondition(rule))
                        dataProvider.ConsumeInputBuffer();
                    ExecuteAction(rule.action, dataProvider);
                    changeStateAction(rule.targetState);
                }
            );
        }


        /// <summary>
        /// 评估所有条件 — 通过 IConditionDataProvider 接口
        /// </summary>
        public static bool EvaluateAll(ConditionData[] conditions, IConditionDataProvider dataProvider)
        {
            if (conditions == null || conditions.Length == 0)
                return true;
            foreach (var condition in conditions)
            {
                if (!Evaluate(condition, dataProvider))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 通过注册表评估单个条件 - O(1) 字典查找替代 switch-case
        /// </summary>
        public static bool Evaluate(ConditionData condition, IConditionDataProvider dataProvider)
        {
            if (condition == null) return false;

            if (_registry.TryGetValue(condition.type, out var evaluator))
            {
                return evaluator(condition, dataProvider);
            }

            return false;
        }

        /// <summary>
        /// 执行转换动作 — 通过 IConditionDataProvider 接口
        /// </summary>
        public static void ExecuteAction(TransitionActionData action, IConditionDataProvider dataProvider)
        {
            if (action == null || action.type == TransitionActionType.None)
                return;

            if (_actionRegistry.TryGetValue(action.type, out var executor))
            {
                executor(action, dataProvider);
            }
        }

        /// <summary>
        /// 检查规则中是否包含 InputPressed 条件（用于 Action 阶段决定是否消耗输入缓冲区）
        /// </summary>
        private static bool HasInputPressedCondition(RuleData rule)
        {
            if (rule?.conditions == null) return false;
            foreach (var c in rule.conditions)
            {
                if (c?.type == ConditionType.InputPressed)
                    return true;
            }
            return false;
        }

        #endregion

        #region Timeline Track System API

        /// <summary>
        /// [时间线轨道系统] 在指定时间内评估单个片段的所有规则
        /// 如果 Timer 在 Clip 的时间窗口内，且任一规则条件满足，则返回该规则
        /// </summary>
        public static bool EvaluateClip(TimeClip clip, float currentTimer, IConditionDataProvider dataProvider, out RuleData matchedRule)
        {
            matchedRule = null;

            // 检查 Timer 是否在 Clip 的时间窗口内
            // 如果 hasUnlimitedEndTime 为 true，则只检查起始时间，不限制结束时间
            if (currentTimer < clip.startTime)
                return false;
            if (!clip.hasUnlimitedEndTime && currentTimer >= clip.endTime)
                return false;

            // 遍历 Clip 内所有规则（OR 逻辑：任一规则条件满足即触发）
            if (clip.rules == null || clip.rules.Length == 0)
                return false;

            foreach (var rule in clip.rules)
            {
                if (EvaluateAll(rule.conditions, dataProvider))
                {
                    matchedRule = rule;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// [时间线轨道系统] 评估单个轨道
        /// 在轨道中找到当前 Timer 所在的 Clip，然后评估其规则
        /// </summary>
        public static bool EvaluateTrack(TransitionTrack track, float currentTimer, IConditionDataProvider dataProvider, out RuleData matchedRule)
        {
            matchedRule = null;

            if (track == null || !track.isActive || track.clips == null || track.clips.Count == 0)
                return false;

            // 遍历轨道中的所有 Clip，找到当前 Timer 所在的 Clip
            foreach (var clip in track.clips)
            {
                if (EvaluateClip(clip, currentTimer, dataProvider, out matchedRule))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// [时间线轨道系统] 按优先级评估所有轨道
        /// 从上到下遍历轨道，找到最先命中的规则
        /// </summary>
        public static bool EvaluateTracks(TransitionTrack[] tracks, float currentTimer, IConditionDataProvider dataProvider,
            out RuleData matchedRule, out string matchedTrackName)
        {
            matchedRule = null;
            matchedTrackName = null;

            if (tracks == null || tracks.Length == 0)
                return false;

            foreach (var track in tracks)
            {
                if (!track.isActive)
                    continue;

                if (EvaluateTrack(track, currentTimer, dataProvider, out matchedRule))
                {
                    matchedTrackName = track.name;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// [时间线轨道系统] 评估全局规则（无时间限制的兜底规则）
        /// </summary>
        public static bool EvaluateGlobalRules(RuleData[] globalRules, IConditionDataProvider dataProvider, out RuleData matchedRule)
        {
            matchedRule = null;

            if (globalRules == null || globalRules.Length == 0)
                return false;

            foreach (var rule in globalRules)
            {
                if (EvaluateAll(rule.conditions, dataProvider))
                {
                    matchedRule = rule;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// [时间线轨道系统] 创建轨道规则的运行时委托数组
        /// 兼容现有的 (Func<bool> Condition, Action Action) 格式
        /// 
        /// [安全性] 使用双评估法替代 cachedMatchedRule 闭包变量：
        /// - Condition 只返回评估结果（bool），不缓存匹配规则
        /// - Action 重新评估轨道以获取匹配规则，然后执行
        /// - 消除闭包状态不一致问题（Condition 返回 true 但 Action 未被执行时状态残留）
        /// - 安全前提：Timer 在同一帧内的 Condition/Action 两次调用之间不会变化
        /// </summary>
        public static (Func<bool> Condition, Action Action)[] CreateTrackDelegates(
            TransitionTrack[] tracks,
            RuleData[] globalRules,
            PlayerBaseBehaviour behaviour,
            Action<PlayerBehaviour> changeStateAction)
        {
            if ((tracks == null || tracks.Length == 0) && (globalRules == null || globalRules.Length == 0))
                return null;

            var delegateList = new List<(Func<bool>, Action)>();
            IConditionDataProvider dataProvider = behaviour;

            // 为每个轨道创建委托
            if (tracks != null)
            {
                foreach (var track in tracks)
                {
                    if (!track.isActive) continue;
                    if (track.clips == null || track.clips.Count == 0) continue;

                    // 捕获 track 和 dataProvider 引用（不缓存 matchedRule）
                    var capturedTrack = track;
                    var capturedDataProvider = dataProvider;

                    delegateList.Add((
                        // Condition: 只评估轨道是否有匹配规则，不缓存结果
                        () =>
                        {
                            var timer = capturedDataProvider.Timer;
                            return EvaluateTrack(capturedTrack, timer, capturedDataProvider, out _);
                        },
                        // Action: 重新评估轨道获取匹配规则并执行（双评估法）
                        () =>
                        {
                            var timer = capturedDataProvider.Timer;
                            if (EvaluateTrack(capturedTrack, timer, capturedDataProvider, out var matchedRule))
                            {
                                // PeekIns 已在条件评估阶段非破坏性检测通过，此时消耗输入缓冲区
                                if (HasInputPressedCondition(matchedRule))
                                    capturedDataProvider.ConsumeInputBuffer();
                                ExecuteAction(matchedRule.action, capturedDataProvider);
                                changeStateAction(matchedRule.targetState);
                            }
                        }
                    ));
                }
            }

            // 为全局规则创建委托
            if (globalRules != null && globalRules.Length > 0)
            {
                var capturedDataProvider = dataProvider;
                foreach (var rule in globalRules)
                {
                    var capturedRule = rule;
                    delegateList.Add((
                        () => EvaluateAll(capturedRule.conditions, capturedDataProvider),
                        () =>
                        {
                            ExecuteAction(capturedRule.action, capturedDataProvider);
                            changeStateAction(capturedRule.targetState);
                        }
                    ));
                }
            }

            return delegateList.ToArray();
        }

        #endregion
    }
}
