using System;
using System.Collections.Generic;
using Sekiro.Character.Data;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 条件类型枚举 - 定义所有可在编辑器中配置的转换条件
    /// </summary>
    public enum ConditionType
    {
        /// <summary>输入按钮按下 (Attack/Parry/Jump/Dodge) </summary>
        InputPressed = 0,
        /// <summary>防御键按住</summary>
        DefenceHeld = 1,
        /// <summary>冲刺键按住</summary>
        SprintHeld = 2,
        /// <summary>接地状态为 true</summary>
        IsGrounded = 5,
        /// <summary>接地状态为 false</summary>
        NotGrounded = 6,
        /// <summary>目标方向 y 分量等于指定值 (+1前/-1后)</summary>
        TargetVectorYEquals = 7,
        /// <summary>动画输入参数幅度为 0</summary>
        AnimInputMagnitudeZero = 8,
        /// <summary>动画输入参数幅度不为 0</summary>
        AnimInputMagnitudeNotZero = 9,
        /// <summary>垂直速度检查 (VerticalSpeed < 0)</summary>
        VerticalSpeedCheck = 11,
        /// <summary>跳跃方向为零 (JumpDirection != Vector2.zero)</summary>
        JumpDirectionZero = 21,
        /// <summary>跳跃方向不为零 (JumpDirection != Vector2.zero)</summary>
        JumpDirectionNotZero = 13,
        /// <summary>冲刺松开计时超时 (Sprint/StartSprint 的 _timer > floatValue)</summary>
        SprintReleaseTimerElapsed = 14,
        /// <summary>防御键未按住</summary>
        NotDefenceHeld = 15,
        /// <summary>非攻击动画播放中 (!AttackGround.VisualIsAttacking)</summary>
        NotAttacking = 18,
        /// <summary>冲刺键按住累计计时 > floatValue (Dodge._timer, 反射读取)</summary>
        SprintHoldTimerElapsed = 20,
        /// <summary>是否左转</summary>
        TurnIsLeft = 22,
        /// <summary>是否右转</summary>
        TurnIsRight = 23,
        /// <summary>攻击阶段匹配（配 attackState 参数）</summary>
        AttackStateMatch = 24,
        /// <summary>招架阶段匹配（配 parryState 参数）</summary>
        ParryStateMatch = 25,
        /// <summary>非招架状态</summary>
        NotParrying = 26,

    }

    /// <summary>
    /// 转换动作类型枚举 - 转换时可执行的附加操作
    /// </summary>
    public enum TransitionActionType
    {
        /// <summary>无操作</summary>
        None = 0,
        /// <summary>设置连击引用 - 直接引用 ComboConfigSO 资产，不依赖 ComboDatabase 列表顺序</summary>
        SetComboReference = 1,
        /// <summary>设置跳跃力 (Variable.JumpForce)</summary>
        SetJumpForce = 2,
    }

    /// <summary>
    /// 可序列化的条件数据 - 存储在 SO 中，可在 Inspector 或 GraphView 中编辑
    /// </summary>
    [Serializable]
    public class ConditionData
    {
        /// <summary>条件类型</summary>
        public ConditionType type;

        /// <summary>
        /// 通用浮点参数
        /// TimerElapsed -> 超时时间
        /// TargetVectorYEquals -> 目标值
        /// </summary>
        public float floatValue;

        /// <summary>通用开关参数</summary>
        public bool boolValue;

        /// <summary>输入类型 (InputPressed)</summary>
        public InputType inputType;

        /// <summary>攻击阶段 (AttackStateMatch)</summary>
        public AttackState attackState;

        /// <summary>招架阶段 (ParryStateMatch)</summary>
        public ParryState parryState;
    }

    /// <summary>
    /// 可序列化的转换动作数据
    /// </summary>
    [Serializable]
    public class TransitionActionData
    {
        public TransitionActionType type = TransitionActionType.None;

        /// <summary>用于 SetComboReference — 直接引用 ComboConfigSO 资产，不依赖 ComboDatabase 列表顺序</summary>
        public ComboConfigSO comboReference;

        /// <summary>用于 SetJumpForce</summary>
        public float floatValue;
    }

    /// <summary>
    /// 可序列化的转换规则 - 包含条件列表 + 动作 + 目标状态
    /// </summary>
    [Serializable]
    public class RuleData
    {
        /// <summary>条件列表（AND 关系，全部满足才触发）</summary>
        public ConditionData[] conditions = Array.Empty<ConditionData>();

        /// <summary>转换时执行的附加动作</summary>
        public TransitionActionData action = new TransitionActionData { type = TransitionActionType.None };

        /// <summary>目标状态</summary>
        public PlayerBehaviour targetState;
    }

    // ====================================================================
    // 时间线轨道系统数据结构 (方案一：时间线轨道系统)
    // ====================================================================

    /// <summary>
    /// 时间片段（Clip） - 类似剪映中的一个视频片段
    /// 代表一段时间窗口内的转换规则集合
    /// </summary>
    [Serializable]
    public class TimeClip
    {
        /// <summary>片段名称（用于编辑器显示）</summary>
        public string name = "New Clip";

        /// <summary>片段起始时间（相对进入状态的 Timer）</summary>
        public float startTime;

        /// <summary>片段结束时间</summary>
        public float endTime = 1f;

        /// <summary>结束时间是否无上限（∞），开启后 endTime 将被忽略</summary>
        public bool hasUnlimitedEndTime;

        /// <summary>该片段内的转换规则列表（AND 逻辑，任一匹配即触发）</summary>
        public RuleData[] rules = Array.Empty<RuleData>();

        /// <summary>片段在编辑器中的显示颜色</summary>
        public Color displayColor = new Color(0.3f, 0.6f, 1f, 0.6f);

        /// <summary>片段时长（只读）</summary>
        public float Duration => endTime - startTime;
    }

    /// <summary>
    /// 转换轨道（Track） - 类似剪映中的一行轨道
    /// 包含多个时间片段，轨道之间按从上到下顺序评估
    /// </summary>
    [Serializable]
    public class TransitionTrack
    {
        /// <summary>轨道名称</summary>
        public string name = "Track 0";


        /// <summary>轨道是否启用</summary>
        public bool isActive = true;

        /// <summary>轨道中的所有时间片段</summary>
        public List<TimeClip> clips = new List<TimeClip>();
    }

    /// <summary>
    /// 状态节点数据 - 存储一个状态的所有转换规则和在 GraphView 中的位置
    /// </summary>
    [Serializable]
    public class StateNodeData
    {
        /// <summary>状态枚举值</summary>
        public PlayerBehaviour state;

        /// <summary>在 GraphView 编辑器中的位置</summary>
        public Vector2 position;

        /// <summary>
        /// 时间线轨道系统
        /// 多行轨道，从上到下代表优先级
        /// 每个轨道内包含时间片段（Clip），Clip 内包含转换规则
        /// </summary>
        public List<TransitionTrack> tracks = new List<TransitionTrack>();

        /// <summary>
        /// 全局规则 - 无时间限制的兜底规则
        /// 在所有轨道的 Clip 都未命中时评估
        /// </summary>
        public RuleData[] globalRules = Array.Empty<RuleData>();

        /// <summary>
        /// 获取所有轨道（按列表顺序，即 UI 中从上到下的排列顺序）
        /// 优先级由轨道在列表中的位置决定（↑↓按钮重排）
        /// </summary>
        public TransitionTrack[] GetTracksSortedByPriority()
        {
            return tracks.ToArray();
        }

        /// <summary>
        /// 在 Inspector 列表中显示状态名称
        /// </summary>
        public override string ToString()
        {
            return state.ToString();
        }
    }

}

