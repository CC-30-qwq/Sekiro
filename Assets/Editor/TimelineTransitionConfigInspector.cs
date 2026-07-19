using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Sekiro.BehaviourMachine;
using Sekiro.Character.Data;
using System;


/// <summary>
/// 时间线轨道系统 Inspector 编辑器
/// 像剪映一样可视化编辑状态转换规则
/// 
/// 布局示意：
/// ┌──────────────────────────────────────────────────────────────┐
/// │ State: Attack_Ground  [轨道数量: 3]  [+ Add Track]          │
/// │ ┌─ Track "Parry Window" (优先级0)  [X删除] ──────────────┐ │
/// │ │ Timer: 0.0s ──[■■■Clip■■■]─────────────────────────▶  │ │
/// │ │                   3.0s                                 │ │
/// │ │ [Windup 0.0-0.4s] [Attack 0.4-1.0s] [Exit 1.5-2.5s]  │ │
/// │ │ [+ Add Clip]                                            │ │
/// │ └────────────────────────────────────────────────────────┘ │
/// │ ┌─ Track "Combo Chain" (优先级1)  [X删除] ──────────────┐ │
/// │ │ [Combo 0.3-0.8s] [Recovery 0.8-1.5s]                  │ │
/// │ │ [+ Add Clip]                                            │ │
/// │ └────────────────────────────────────────────────────────┘ │
/// │ ┌─ Global Rules ─────────────────────────────────────────┐ │
/// │ │ TimerElapsed(3.0) → Idle                                │ │
/// │ │ [+ Add Global Rule]                                     │ │
/// │ └────────────────────────────────────────────────────────┘ │
/// └──────────────────────────────────────────────────────────────┘
/// </summary>
[CustomEditor(typeof(StateTransitionConfigSO))]
public class TimelineTransitionConfigInspector : Editor
{
    #region Constants

    private const float TIMELINE_HEIGHT = 40f;
    private const float TIMELINE_MARGIN = 20f;
    private const float CLIP_HEIGHT = 30f;
    private const float CLIP_Y_OFFSET = 5f;
    private const float TRACK_FOLDOUT_HEIGHT = 22f;
    private const float MIN_CLIP_WIDTH = 20f;
    private const float TIMELINE_RULER_HEIGHT = 20f;

    #endregion

    #region State

    private StateTransitionConfigSO _config;
    private Vector2 _scrollPosition;
    private int _selectedStateIndex = -1;
    private string[] _stateNames;
    private PlayerBehaviour[] _stateValues;

    // Foldout tracking
    private readonly Dictionary<int, bool> _trackFoldouts = new Dictionary<int, bool>();
    private readonly Dictionary<string, bool> _clipFoldouts = new Dictionary<string, bool>();
    private bool _showGlobalRules = true;

    // Drag state
    private bool _isDraggingClip;
    private TimeClip _draggedClip;
    private int _draggedTrackIndex;
    private int _draggedClipIndex;
    private float _dragStartMouseX;
    private float _dragStartTime;
    private float _mouseDownTime;
    private bool _draggingLeftEdge; // 拖拽左边缘（true）或右边缘（false）

    // Scroll/zoom
    private float _timelineDuration = 3f;
    private float _timelineStartTime = 0f;

    #endregion

    #region Colors

    private static readonly Color TrackBgColor = new Color(0.15f, 0.15f, 0.17f);
    private static readonly Color ClipBorderColor = new Color(0.5f, 0.8f, 1f, 0.8f);
    private static readonly Color RulerColor = new Color(0.3f, 0.3f, 0.35f);
    private static readonly Color RulerTickColor = new Color(0.6f, 0.6f, 0.7f);
    private static readonly Color PlayheadColor = new Color(1f, 0.3f, 0.1f);
    private static readonly Color AddButtonColor = new Color(0.2f, 0.5f, 0.2f);
    private static readonly Color DeleteButtonColor = new Color(0.6f, 0.2f, 0.2f);

    #endregion

    private void OnEnable()
    {
        _config = target as StateTransitionConfigSO;
        RefreshStateNames();
    }

    private void RefreshStateNames()
    {
        var values = Enum.GetValues(typeof(PlayerBehaviour));
        _stateValues = new PlayerBehaviour[values.Length];
        _stateNames = new string[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            _stateValues[i] = (PlayerBehaviour)values.GetValue(i);
            _stateNames[i] = GetStateDisplayName(_stateValues[i]);
        }
    }

    private string GetStateDisplayName(PlayerBehaviour state)
    {
        var fieldInfo = typeof(PlayerBehaviour).GetField(state.ToString());
        var attrs = fieldInfo?.GetCustomAttributes(typeof(DisplayNameAttribute), false);
        if (attrs != null && attrs.Length > 0)
        {
            return ((DisplayNameAttribute)attrs[0]).DisplayName;
        }
        return state.ToString();
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawToolbar();

        if (_selectedStateIndex >= 0 && _selectedStateIndex < _stateValues.Length)
        {
            var state = _stateValues[_selectedStateIndex];
            DrawStateTimelineEditor(state);
        }
        else
        {
            EditorGUILayout.HelpBox("请选择一个状态进行编辑", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();

        // 标记 SO 为 dirty，确保编辑时保存
        if (GUI.changed)
        {
            EditorUtility.SetDirty(_config);
            // 通知所有 VisualConfigBehaviour 版本变化
            NotifyConfigChanged();
        }
    }

    /// <summary>
    /// 通知场景中所有 VisualConfigBehaviour 配置已变化
    /// </summary>
    private void NotifyConfigChanged()
    {
        var allVisualConfigs = FindObjectsOfType<VisualConfigBehaviour>();
        foreach (var vc in allVisualConfigs)
        {
            if (vc.Config == _config)
            {
                vc.InvalidateVersion();
            }
        }
    }

    #region Toolbar

    private void DrawToolbar()
    {
        EditorGUILayout.Space(5);
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("状态：", GUILayout.Width(50));
        int newIndex = EditorGUILayout.Popup(_selectedStateIndex, _stateNames, EditorStyles.toolbarPopup, GUILayout.Width(200));
        if (newIndex != _selectedStateIndex)
        {
            _selectedStateIndex = newIndex;
            // 清空 foldout 缓存
            _trackFoldouts.Clear();
            _clipFoldouts.Clear();
        }

        GUILayout.FlexibleSpace();

        // 时间线缩放控制
        GUILayout.Label("时间轴:", EditorStyles.miniLabel, GUILayout.Width(50));
        if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(25)))
        {
            _timelineDuration = Mathf.Max(1f, _timelineDuration - 0.5f);
        }
        GUILayout.Label($"{_timelineDuration:F1}s", EditorStyles.miniLabel, GUILayout.Width(35));
        if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(25)))
        {
            _timelineDuration = Mathf.Min(20f, _timelineDuration + 0.5f);
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.Space(3);
    }

    #endregion

    #region State Timeline Editor

    private void DrawStateTimelineEditor(PlayerBehaviour state)
    {
        var nodeData = _config.GetOrCreateNodeData(state);

        // 头部统计
        EditorGUILayout.BeginHorizontal();
        int totalClips = 0;
        if (nodeData.tracks != null)
        {
            foreach (var t in nodeData.tracks)
                totalClips += t.clips?.Count ?? 0;
        }
        EditorGUILayout.LabelField($"状态: {GetStateDisplayName(state)}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"轨道: {nodeData.tracks?.Count ?? 0}, 片段: {totalClips}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        // 添加轨道按钮（自动附带一个默认片段）
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 添加轨道 (Track)", GUILayout.Height(24)))
        {
            if (nodeData.tracks == null)
                nodeData.tracks = new List<TransitionTrack>();
            nodeData.tracks.Add(new TransitionTrack
            {
                name = $"Track {nodeData.tracks.Count}",
                isActive = true,
                clips = new List<TimeClip>
                {
                    new TimeClip
                    {
                        name = "Clip 1",
                        startTime = 0f,
                        endTime = Mathf.Min(0.5f, _timelineDuration),
                        rules = new RuleData[0],
                        displayColor = GetNextClipColor(0)
                    }
                }
            });
            _trackFoldouts[nodeData.tracks.Count - 1] = true;
            GUI.changed = true;
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 绘制时间线标尺
        DrawTimelineRuler(state);

        // 绘制每个轨道
        if (nodeData.tracks != null)
        {
            for (int i = 0; i < nodeData.tracks.Count; i++)
            {
                DrawTrackEditor(nodeData, i);
            }
        }

        // 全局规则
        EditorGUILayout.Space(5);
        DrawGlobalRulesEditor(nodeData);
    }

    #endregion

    #region Timeline Ruler

    private void DrawTimelineRuler(PlayerBehaviour state)
    {
        var rulerRect = GUILayoutUtility.GetRect(Screen.width, TIMELINE_RULER_HEIGHT);
        float rulerWidth = rulerRect.width - TIMELINE_MARGIN;
        float rulerLeft = rulerRect.x + TIMELINE_MARGIN;

        // 背景
        EditorGUI.DrawRect(rulerRect, RulerColor);

        // 标尺刻度
        Handles.BeginGUI();
        Handles.color = RulerTickColor;

        float totalDuration = _timelineDuration;
        float pixelsPerSecond = rulerWidth / totalDuration;

        // 主刻度（每秒）
        for (float t = 0; t <= totalDuration; t += 0.5f)
        {
            float x = rulerLeft + t * pixelsPerSecond;
            bool isMajorTick = Mathf.Approximately(t % 1f, 0f);

            // 刻度线
            float tickHeight = isMajorTick ? 10f : 5f;
            Handles.DrawLine(new Vector3(x, rulerRect.y + rulerRect.height), new Vector3(x, rulerRect.y + rulerRect.height - tickHeight));

            // 标签
            if (isMajorTick)
            {
                GUI.Label(new Rect(x - 15, rulerRect.y, 30, rulerRect.height), $"{t:F0}s", EditorStyles.miniLabel);
            }
        }

        Handles.EndGUI();

        // 保存布局参数供后续使用
        _timelineStartTime = 0f;
    }

    #endregion

    #region Track Editor

    private void DrawTrackEditor(StateNodeData nodeData, int trackIndex)
    {
        var track = nodeData.tracks[trackIndex];
        if (track == null) return;

        // 自动生成轨道名 = clip的目标状态名
        if (track.clips != null && track.clips.Count > 0 && track.clips[0].rules != null && track.clips[0].rules.Length > 0)
        {
            track.name = $"→ {track.clips[0].rules[0].targetState}";
        }
        else
        {
            track.name = "→ Idle";
        }

        // 轨道容器背景
        EditorGUILayout.BeginVertical(GUI.skin.box);

        // 轨道头部
        EditorGUILayout.BeginHorizontal();

        // Foldout 折叠
        bool foldout = true;
        _trackFoldouts.TryGetValue(trackIndex, out foldout);
        foldout = EditorGUILayout.Foldout(foldout, $"", true);
        _trackFoldouts[trackIndex] = foldout;

        // 轨道名称（自动跟随目标状态，不可编辑）
        GUILayout.Label(track.name, GUILayout.Width(120));

        // 显示轨道序号（仅用于标识，不可编辑）
        GUILayout.Label($"#{trackIndex}", EditorStyles.miniLabel, GUILayout.Width(25));

        // 启用开关
        track.isActive = EditorGUILayout.ToggleLeft("启用", track.isActive, GUILayout.Width(50));

        GUILayout.FlexibleSpace();

        // 向上移动
        if (trackIndex > 0 && GUILayout.Button("↑", GUILayout.Width(25)))
        {
            nodeData.tracks[trackIndex] = nodeData.tracks[trackIndex - 1];
            nodeData.tracks[trackIndex - 1] = track;
            GUI.changed = true;
            return;
        }
        // 向下移动
        if (trackIndex < nodeData.tracks.Count - 1 && GUILayout.Button("↓", GUILayout.Width(25)))
        {
            nodeData.tracks[trackIndex] = nodeData.tracks[trackIndex + 1];
            nodeData.tracks[trackIndex + 1] = track;
            GUI.changed = true;
            return;
        }

        // 删除轨道
        GUI.color = DeleteButtonColor;
        if (GUILayout.Button("✕", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("删除轨道", $"确定删除轨道 \"{track.name}\" 吗？", "删除", "取消"))
            {
                nodeData.tracks.RemoveAt(trackIndex);
                _trackFoldouts.Remove(trackIndex);
                GUI.changed = true;
                return;
            }
        }
        GUI.color = Color.white;

        EditorGUILayout.EndHorizontal();

        if (foldout)
        {
            EditorGUILayout.Space(2);

            // 时间线画布
            DrawTrackTimeline(track, trackIndex);

            // 片段列表（折叠面板）
            for (int clipIndex = 0; clipIndex < track.clips.Count; clipIndex++)
            {
                DrawClipEditor(track, clipIndex, trackIndex);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    #endregion

    #region Track Timeline Canvas

    private void DrawTrackTimeline(TransitionTrack track, int trackIndex)
    {
        float canvasHeight = TIMELINE_HEIGHT;
        var canvasRect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, canvasHeight);
        float canvasWidth = canvasRect.width - TIMELINE_MARGIN;
        float canvasLeft = canvasRect.x + TIMELINE_MARGIN;

        // 背景
        EditorGUI.DrawRect(canvasRect, TrackBgColor);
        EditorGUI.DrawRect(new Rect(canvasLeft, canvasRect.y, canvasWidth, canvasRect.height), new Color(0.12f, 0.12f, 0.14f));

        // 时间线线
        Handles.BeginGUI();
        Handles.color = new Color(0.25f, 0.25f, 0.3f);
        float lineY = canvasRect.y + canvasRect.height * 0.5f;
        Handles.DrawLine(new Vector3(canvasLeft, lineY), new Vector3(canvasLeft + canvasWidth, lineY));
        Handles.EndGUI();

        float pixelsPerSecond = canvasWidth / _timelineDuration;

        // 绘制 Clip
        for (int clipIndex = 0; clipIndex < track.clips.Count; clipIndex++)
        {
            var clip = track.clips[clipIndex];
            DrawClipOnTimeline(clip, clipIndex, trackIndex, canvasLeft, lineY, pixelsPerSecond, canvasHeight, track);
        }

        // 处理鼠标事件（拖拽创建新 Clip）
        HandleTimelineMouseEvents(canvasRect, canvasLeft, canvasWidth, pixelsPerSecond, track);
    }

    private void DrawClipOnTimeline(TimeClip clip, int clipIndex, int trackIndex,
        float canvasLeft, float lineY, float pixelsPerSecond, float canvasHeight, TransitionTrack track)
    {
        float timelineRightEdge = canvasLeft + (_timelineDuration * pixelsPerSecond);

        // 无上限时间：Clip 延伸到画布最右端
        bool isUnlimited = clip.hasUnlimitedEndTime;

        float clipX = canvasLeft + clip.startTime * pixelsPerSecond;
        float clipWidth;
        if (isUnlimited)
        {
            clipWidth = Mathf.Max(MIN_CLIP_WIDTH, timelineRightEdge - clipX);
        }
        else
        {
            clipWidth = Mathf.Max(MIN_CLIP_WIDTH, clip.Duration * pixelsPerSecond);
        }

        var clipRect = new Rect(clipX, lineY - CLIP_HEIGHT * 0.5f + CLIP_Y_OFFSET, clipWidth, CLIP_HEIGHT);

        // 确保在可见范围内
        if (clipRect.x > timelineRightEdge) return;
        if (clipRect.x + clipRect.width < canvasLeft) return;

        // Clip 背景
        Color clipColor = clip.displayColor;
        clipColor.a = 0.7f;
        EditorGUI.DrawRect(clipRect, clipColor);

        // Clip 边框（无上限时右边缘用虚线箭头风格）
        Handles.BeginGUI();
        Handles.color = ClipBorderColor;
        Handles.DrawLine(new Vector3(clipRect.x, clipRect.y), new Vector3(clipRect.x, clipRect.y + clipRect.height));
        if (!isUnlimited)
        {
            Handles.DrawLine(new Vector3(clipRect.x + clipRect.width, clipRect.y), new Vector3(clipRect.x + clipRect.width, clipRect.y + clipRect.height));
        }
        else
        {
            // 无上限：右边缘用箭头指示 ∞
            float rightX = clipRect.x + clipRect.width;
            float midY = clipRect.y + clipRect.height * 0.5f;
            Handles.DrawLine(new Vector3(rightX, clipRect.y), new Vector3(rightX - 5, clipRect.y + 5));
            Handles.DrawLine(new Vector3(rightX, clipRect.y), new Vector3(rightX - 5, clipRect.y - 5));
            Handles.DrawLine(new Vector3(rightX, clipRect.y + clipRect.height), new Vector3(rightX - 5, clipRect.y + clipRect.height - 5));
            Handles.DrawLine(new Vector3(rightX, clipRect.y + clipRect.height), new Vector3(rightX - 5, clipRect.y + clipRect.height + 5));
        }
        Handles.EndGUI();

        // Clip 文字（显示目标状态名，无上限时附加 ∞ 标记）
        string label = isUnlimited ? $"{clip.name} ∞" : clip.name;
        GUI.Label(clipRect, label, new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            normal = { textColor = Color.white }
        });

        // Clip 两侧拖拽手柄（起始/结束时间）
        // 左手柄
        var leftHandleRect = new Rect(clipRect.x - 3, clipRect.y, 6, clipRect.height);
        EditorGUIUtility.AddCursorRect(leftHandleRect, MouseCursor.ResizeHorizontal);
        if (Event.current.type == EventType.MouseDown && leftHandleRect.Contains(Event.current.mousePosition))
        {
            // 拖拽起始时间（左边缘）
            _isDraggingClip = true;
            _draggingLeftEdge = true;
            _draggedClip = clip;
            _draggedTrackIndex = trackIndex;
            _draggedClipIndex = clipIndex;
            _dragStartMouseX = Event.current.mousePosition.x;
            _dragStartTime = clip.startTime;
            Event.current.Use();
        }

        // 右手柄（无上限时不显示拖拽手柄）
        if (!isUnlimited)
        {
            var rightHandleRect = new Rect(clipRect.x + clipRect.width - 3, clipRect.y, 6, clipRect.height);
            EditorGUIUtility.AddCursorRect(rightHandleRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && rightHandleRect.Contains(Event.current.mousePosition))
            {
                // 拖拽结束时间（右边缘）
                _isDraggingClip = true;
                _draggingLeftEdge = false;
                _draggedClip = clip;
                _draggedTrackIndex = trackIndex;
                _draggedClipIndex = clipIndex;
                _dragStartMouseX = Event.current.mousePosition.x;
                _dragStartTime = clip.endTime;
                Event.current.Use();
            }
        }

        // 点击 Clip 主体选中（不触发拖拽）
        if (Event.current.type == EventType.MouseDown && clipRect.Contains(Event.current.mousePosition) && !_isDraggingClip)
        {
            _draggedClip = clip;
            _draggedTrackIndex = trackIndex;
            _draggedClipIndex = clipIndex;
            Event.current.Use();
        }

        // 拖拽逻辑
        if (_isDraggingClip && _draggedClip == clip && Event.current.type == EventType.MouseDrag)
        {
            float deltaPixels = Event.current.mousePosition.x - _dragStartMouseX;
            float deltaTime = deltaPixels / pixelsPerSecond;

            if (_draggingLeftEdge)
            {
                // 拖拽左边缘（起始时间）
                clip.startTime = Mathf.Max(0, _dragStartTime + deltaTime);
                if (!isUnlimited && clip.startTime >= clip.endTime)
                    clip.startTime = clip.endTime - 0.05f;
            }
            else
            {
                // 拖拽右边缘（结束时间）
                clip.endTime = Mathf.Max(clip.startTime + 0.05f, _dragStartTime + deltaTime);
            }

            GUI.changed = true;
            Event.current.Use();
        }

        // 释放
        if (_isDraggingClip && _draggedClip == clip && Event.current.type == EventType.MouseUp)
        {
            _isDraggingClip = false;
            _draggingLeftEdge = false;
            _draggedClip = null;
            Event.current.Use();
        }
    }

    private void HandleTimelineMouseEvents(Rect canvasRect, float canvasLeft, float canvasWidth,
        float pixelsPerSecond, TransitionTrack track)
    {
        // 轨道创建时已自动附带片段，右键不再需要创建 Clip
    }

    private Color GetNextClipColor(int index)
    {
        var colors = new[]
        {
            new Color(0.3f, 0.6f, 1f, 0.6f),
            new Color(0.3f, 1f, 0.6f, 0.6f),
            new Color(1f, 0.6f, 0.3f, 0.6f),
            new Color(0.8f, 0.3f, 1f, 0.6f),
            new Color(1f, 0.3f, 0.3f, 0.6f),
            new Color(0.3f, 1f, 1f, 0.6f),
        };
        return colors[index % colors.Length];
    }

    #endregion

    #region Clip Editor

    private void DrawClipEditor(TransitionTrack track, int clipIndex, int trackIndex)
    {
        var clip = track.clips[clipIndex];
        if (clip == null) return;

        // 确保每个clip有且只有一条规则
        if (clip.rules == null || clip.rules.Length == 0)
        {
            clip.rules = new RuleData[]
            {
                new RuleData
                {
                    conditions = new ConditionData[0],
                    action = new TransitionActionData { type = TransitionActionType.None },
                    targetState = PlayerBehaviour.Idle
                }
            };
        }
        var rule = clip.rules[0];

        // Clip名称自动跟随目标状态
        clip.name = $"→ {rule.targetState}";

        string clipKey = $"{trackIndex}_{clipIndex}";
        bool foldout = true;
        _clipFoldouts.TryGetValue(clipKey, out foldout);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.BeginHorizontal();

        _clipFoldouts[clipKey] = EditorGUILayout.Foldout(foldout, $"", true);

        // 片段名称（自动显示目标状态，不可编辑）
        GUILayout.Label(clip.name, GUILayout.Width(100));

        // 时间段
        GUILayout.Label("时间:", GUILayout.Width(35));
        clip.startTime = EditorGUILayout.FloatField(clip.startTime, GUILayout.Width(45));
        GUILayout.Label("~", GUILayout.Width(10));

        // 无上限时间切换
        clip.hasUnlimitedEndTime = GUILayout.Toggle(clip.hasUnlimitedEndTime, "∞", GUILayout.Width(25));
        if (!clip.hasUnlimitedEndTime)
        {
            clip.endTime = EditorGUILayout.FloatField(clip.endTime, GUILayout.Width(45));
            GUILayout.Label($"({clip.Duration:F2}s)", EditorStyles.miniLabel);
        }
        else
        {
            GUILayout.Label("∞", EditorStyles.miniLabel, GUILayout.Width(50));
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        if (foldout)
        {
            // 目标状态
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("目标状态:", GUILayout.Width(70));
            rule.targetState = (PlayerBehaviour)EditorGUILayout.EnumPopup(rule.targetState, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            // 条件和动作
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label($"条件: {rule.conditions?.Length ?? 0}", EditorStyles.miniLabel);
            if (GUILayout.Button("编辑条件", GUILayout.Width(60)))
                ShowConditionEditor(rule);
            GUILayout.Space(10);
            if (GUILayout.Button("动作", GUILayout.Width(40)))
                ShowActionEditor(rule);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(1);
    }

    #endregion

    #region Global Rules Editor

    private void DrawGlobalRulesEditor(StateNodeData nodeData)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        _showGlobalRules = EditorGUILayout.Foldout(_showGlobalRules, "全局规则 (Global Rules) - 无时间限制的兜底规则", true);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ 添加全局规则", GUILayout.Height(20)))
        {
            var rulesList = new List<RuleData>(nodeData.globalRules ?? Array.Empty<RuleData>());
            rulesList.Add(new RuleData
            {
                conditions = new ConditionData[0],
                action = new TransitionActionData { type = TransitionActionType.None },
                targetState = PlayerBehaviour.Idle
            });
            nodeData.globalRules = rulesList.ToArray();
            GUI.changed = true;
        }
        EditorGUILayout.EndHorizontal();

        if (_showGlobalRules && nodeData.globalRules != null)
        {
            var globalRulesList = new List<RuleData>(nodeData.globalRules);
            for (int i = 0; i < globalRulesList.Count; i++)
            {
                int capturedIndex = i;

                // 内联绘制全局规则（带 ↑↓ 按钮）
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();

                // ↑ 按钮 — 仅非第一个时启用
                if (i > 0 && GUILayout.Button("↑", GUILayout.Width(22)))
                {
                    var temp = globalRulesList[i];
                    globalRulesList[i] = globalRulesList[i - 1];
                    globalRulesList[i - 1] = temp;
                    nodeData.globalRules = globalRulesList.ToArray();
                    GUI.changed = true;
                    return;
                }
                // ↓ 按钮 — 仅非最后一个时启用
                if (i < globalRulesList.Count - 1 && GUILayout.Button("↓", GUILayout.Width(22)))
                {
                    var temp = globalRulesList[i];
                    globalRulesList[i] = globalRulesList[i + 1];
                    globalRulesList[i + 1] = temp;
                    nodeData.globalRules = globalRulesList.ToArray();
                    GUI.changed = true;
                    return;
                }
                // 若两个按钮都未显示，留出占位空间保持对齐
                if (i == 0 && globalRulesList.Count == 1)
                    GUILayout.Space(44);

                GUILayout.Label($"#{i}", GUILayout.Width(20));

                // 目标状态
                GUILayout.Label("→", GUILayout.Width(15));
                globalRulesList[i].targetState = (PlayerBehaviour)EditorGUILayout.EnumPopup(globalRulesList[i].targetState, GUILayout.Width(120));

                GUILayout.Label($"条件: {globalRulesList[i].conditions?.Length ?? 0}", EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("编辑条件", GUILayout.Width(60)))
                    ShowConditionEditor(globalRulesList[i]);
                if (GUILayout.Button("动作", GUILayout.Width(40)))
                    ShowActionEditor(globalRulesList[i]);

                // 删除
                GUI.color = DeleteButtonColor;
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    globalRulesList.RemoveAt(i);
                    nodeData.globalRules = globalRulesList.ToArray();
                    GUI.changed = true;
                    return;
                }
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (nodeData.globalRules.Length == 0)
            {
                EditorGUILayout.HelpBox("暂无全局规则", MessageType.None);
            }
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Condition/Action Popup Editors

    private void ShowConditionEditor(RuleData rule)
    {
        // 使用泛型窗口编辑条件
        ConditionEditorWindow.Open(rule);
    }

    private void ShowActionEditor(RuleData rule)
    {
        ActionEditorWindow.Open(rule);
    }

    #endregion
}

// ============================================================================
// 条件编辑器窗口
// ============================================================================
public class ConditionEditorWindow : EditorWindow
{
    private RuleData _rule;
    private Vector2 _scrollPos;

    public static void Open(RuleData rule)
    {
        var window = GetWindow<ConditionEditorWindow>(true, "编辑条件", true);
        window._rule = rule;
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        if (_rule == null) return;

        EditorGUILayout.LabelField("转换条件编辑", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        var conditions = _rule.conditions;
        if (conditions == null)
        {
            _rule.conditions = Array.Empty<ConditionData>();
            conditions = _rule.conditions;
        }

        // 条件列表
        for (int i = 0; i < conditions.Length; i++)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"条件 {i + 1}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // 删除条件
            GUI.color = new Color(0.6f, 0.2f, 0.2f);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                var list = new System.Collections.Generic.List<ConditionData>(conditions);
                list.RemoveAt(i);
                _rule.conditions = list.ToArray();
                GUI.changed = true;
                return;
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            // 条件类型
            conditions[i].type = (ConditionType)EditorGUILayout.EnumPopup("类型", conditions[i].type);

            // 根据条件类型显示不同参数
            switch (conditions[i].type)
            {
                case ConditionType.InputPressed:
                    conditions[i].inputType = (InputType)EditorGUILayout.EnumPopup("输入类型", conditions[i].inputType);
                    break;
                case ConditionType.TargetVectorYEquals:
                    conditions[i].floatValue = EditorGUILayout.FloatField("目标值", conditions[i].floatValue);
                    break;
                case ConditionType.SprintReleaseTimerElapsed:
                case ConditionType.SprintHoldTimerElapsed:
                    conditions[i].floatValue = EditorGUILayout.FloatField("阈值(秒)", conditions[i].floatValue);
                    break;
                case ConditionType.DefenceHeld:
                case ConditionType.SprintHeld:
                case ConditionType.NotDefenceHeld:
                case ConditionType.IsGrounded:
                case ConditionType.NotGrounded:
                case ConditionType.AnimInputMagnitudeZero:
                case ConditionType.AnimInputMagnitudeNotZero:
                case ConditionType.VerticalSpeedCheck:
                case ConditionType.JumpDirectionZero:
                case ConditionType.JumpDirectionNotZero:
                case ConditionType.NotAttacking:
                case ConditionType.NotParrying:
                case ConditionType.TurnIsLeft:
                case ConditionType.TurnIsRight:
                    // 无参数条件
                    break;
                case ConditionType.AttackStateMatch:
                    conditions[i].attackState = (AttackState)EditorGUILayout.EnumPopup("攻击阶段", conditions[i].attackState);
                    break;
                case ConditionType.ParryStateMatch:
                    conditions[i].parryState = (ParryState)EditorGUILayout.EnumPopup("招架阶段", conditions[i].parryState);
                    break;
                default:
                    conditions[i].floatValue = EditorGUILayout.FloatField("浮点参数", conditions[i].floatValue);
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();

        // 添加条件按钮
        if (GUILayout.Button("+ 添加条件", GUILayout.Height(30)))
        {
            var list = new System.Collections.Generic.List<ConditionData>(conditions);
            list.Add(new ConditionData { type = ConditionType.InputPressed, inputType = InputType.Attack });
            _rule.conditions = list.ToArray();
            GUI.changed = true;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("多个条件之间是 AND（与）关系，所有条件满足才触发转换", MessageType.Info);
    }
}

// ============================================================================
// 动作编辑器窗口
// ============================================================================
public class ActionEditorWindow : EditorWindow
{
    private RuleData _rule;

    public static void Open(RuleData rule)
    {
        var window = GetWindow<ActionEditorWindow>(true, "编辑动作", true);
        window._rule = rule;
        window.minSize = new Vector2(350, 150);
        window.Show();
    }

    private void OnGUI()
    {
        if (_rule == null) return;

        EditorGUILayout.LabelField("转换动作编辑", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (_rule.action == null)
            _rule.action = new TransitionActionData { type = TransitionActionType.None };

        _rule.action.type = (TransitionActionType)EditorGUILayout.EnumPopup("动作类型", _rule.action.type);

        switch (_rule.action.type)
        {
            case TransitionActionType.SetComboReference:
                _rule.action.comboReference = (ComboConfigSO)EditorGUILayout.ObjectField("连击配置", _rule.action.comboReference, typeof(ComboConfigSO), false);
                break;
            case TransitionActionType.SetJumpForce:
                _rule.action.floatValue = EditorGUILayout.FloatField("跳跃力", _rule.action.floatValue);
                break;
            case TransitionActionType.None:
                EditorGUILayout.HelpBox("不执行附加动作", MessageType.Info);
                break;
        }
    }
}
