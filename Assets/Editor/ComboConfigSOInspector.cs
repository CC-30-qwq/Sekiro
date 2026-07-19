using UnityEngine;
using UnityEditor;
using Sekiro.Character.Data;
using Sekiro.BehaviourMachine;
using System.Linq;

[CustomEditor(typeof(ComboConfigSO))]
public class ComboConfigSOInspector : Editor
{
    #region Phase Meta - 数据驱动阶段配置

    /// <summary>
    /// 阶段元数据 — 新增阶段只需在此数组中追加一项，所有绘制/事件代码自动适配
    /// </summary>
    private class PhaseMeta
    {
        public readonly string NameCN;
        public readonly string NameEN;
        public readonly string PropertyName; // ComboConfigSO 中的字段名，用于 FindProperty
        public readonly Color Color;
        public readonly Color BgColor;

        public PhaseMeta(string nameCN, string nameEN, string propName, Color color, Color bgColor)
        {
            NameCN = nameCN;
            NameEN = nameEN;
            PropertyName = propName;
            Color = color;
            BgColor = bgColor;
        }
    }

    private static readonly PhaseMeta[] Phases =
    {
        new("蓄力",   "Charging",  "ChargeTime",     new Color(0.25f, 0.50f, 1.00f, 0.75f), new Color(0.15f, 0.25f, 0.45f, 0.40f)),
        new("前摇",   "Windup",    "WindupTime",     new Color(1.00f, 0.75f, 0.15f, 0.75f), new Color(0.45f, 0.35f, 0.10f, 0.40f)),
        new("攻击",   "Attacking", "AttackDuration", new Color(1.00f, 0.25f, 0.20f, 0.75f), new Color(0.45f, 0.15f, 0.10f, 0.40f)),
        new("后摇",   "Recovery",  "RecoveryTime",   new Color(0.20f, 0.80f, 0.35f, 0.75f), new Color(0.10f, 0.35f, 0.18f, 0.40f)),
        new("收招",   "Exit",      "ExitTime",       new Color(0.55f, 0.55f, 0.55f, 0.70f), new Color(0.25f, 0.25f, 0.25f, 0.40f)),
    };

    private int PhaseCount => Phases.Length;

    #endregion

    #region Style Cache - 避免每帧 new GUIStyle

    private static class Styles
    {
        public static readonly GUIStyle PhaseLabel = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9
        };

        public static readonly GUIStyle RulerTickLabel = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 9,
            normal = { textColor = RulerTickColor }
        };

        public static readonly GUIStyle PhaseLabelOnBar = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        public static readonly GUIStyle PreviewHint = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f, 0.8f) },
            alignment = TextAnchor.MiddleCenter
        };
    }

    #endregion

    #region Constants

    // 模型路径 — 通过 EditorPrefs 可配置，无需改源码
    private const string MODEL_PATH_KEY = "ComboConfigSOInspector_DefaultModelPath";
    private const string DEFAULT_MODEL_PATH = "Assets/Character/PlayerResources/Sekiro/Model/Sekiro.fbx";

    private static string GetModelPath()
    {
        return EditorPrefs.GetString(MODEL_PATH_KEY, DEFAULT_MODEL_PATH);
    }

    private const float TIMELINE_RULER_HEIGHT = 22f;
    private const float TIMELINE_BAR_HEIGHT = 32f;
    private const float TIMELINE_MARGIN_LEFT = 12f;
    private const float TIMELINE_MARGIN_RIGHT = 12f;
    private const float DRAG_HANDLE_WIDTH = 7f;
    private const float PREVIEW_HEIGHT = 220f;

    private const float CAM_DEFAULT_DISTANCE = 4.5f;
    private const float CAM_DEFAULT_LOOKAT_Y = 0.9f;
    private const float CAM_DEFAULT_FOV = 35f;
    private const float CAM_ZOOM_SPEED = 0.5f;
    private const float CAM_ROTATE_SPEED = 0.4f;
    private const float CAM_PAN_SPEED = 0.005f;

    private static readonly Color BoundaryLineColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);
    private static readonly Color BoundaryHoverColor = new Color(1f, 1f, 0.3f, 1f);
    private static readonly Color PlayheadColor = new Color(1f, 0.25f, 0.05f, 1f);
    private static readonly Color RulerBgColor = new Color(0.18f, 0.18f, 0.20f);
    private static readonly Color RulerTickColor = new Color(0.55f, 0.55f, 0.60f);
    private static readonly Color HitColliderWindowColor = new Color(1f, 1f, 0.2f, 0.85f);
    private static readonly Color HitColliderWindowOutline = new Color(1f, 0.85f, 0f, 1f);
    private static readonly Color HitColliderMiniBarBg = new Color(0.15f, 0.15f, 0.12f);

    private static readonly Color PreviewBgColor = new Color(0.15f, 0.15f, 0.17f);
    private const float HITCOLLIDER_BAR_HEIGHT = 8f;

    #endregion

    #region Fields

    private ComboConfigSO _config;
    private bool _showBasicProperties = true;
    private bool _showTimeline = true;
    private bool _showComboLinks = true;

    // ⭐ 方案A：自动收集 SerializedProperty 数组，消除 10 个冗余字段
    private SerializedProperty[] _phaseProps;
    private SerializedProperty _animationClipNameProp;
    private SerializedProperty _attackTypeProp;
    private SerializedProperty _attackStrengthProp;
    private SerializedProperty _nextComboOnAttackProp;
    private SerializedProperty _nextComboOnReleaseProp;
    private SerializedProperty _hitColliderWindowsProp;
    private SerializedProperty _rangedHitWindowsProp;

    // ⭐ 鼠标交互模式枚举 — 替换分散的 _dragTarget / _isDraggingScrubber / _isDraggingCamera
    private enum InteractionMode { None, DraggingBoundary, DraggingScrubber, DraggingCamera }
    private InteractionMode _interactionMode = InteractionMode.None;
    private int _dragBoundaryIndex = -1; // 当前拖拽的边界索引 (1 ~ PhaseCount-1)
    private float _dragStartMouseX;
    private float[] _dragStartPhaseValues;

    private PreviewRenderUtility _previewRenderUtility;
    private GameObject _previewInstance;
    private AnimationClip _currentClip;
    private double _previewTime;
    private bool _isPlaying;
    private double _lastEditorTime;
    private bool _previewNeedsRebuild = true;

    private float _orbitYaw = 180f;
    private float _orbitPitch = 15f;
    private float _orbitDistance = CAM_DEFAULT_DISTANCE;
    private float _cameraFov = CAM_DEFAULT_FOV;
    private float _cameraFarClipPlane = 10f;
    private float _cameraZoomSpeed = CAM_ZOOM_SPEED;
    private Vector3 _orbitLookAt = new Vector3(0f, CAM_DEFAULT_LOOKAT_Y, 0f);
    private Vector2 _cameraDragLastMouse;
    private bool _isHoveringPreview;

    private GameObject _scenePreviewModel;

    #endregion

    #region Enable / Disable

    private void OnEnable()
    {
        _config = target as ComboConfigSO;

        // ⭐ 自动收集阶段 SerializedProperty 数组
        _phaseProps = Phases.Select(p => serializedObject.FindProperty(p.PropertyName)).ToArray();

        _animationClipNameProp = serializedObject.FindProperty("AnimationClipName");
        _attackTypeProp = serializedObject.FindProperty("AttackType");
        _attackStrengthProp = serializedObject.FindProperty("AttackStrength");
        _nextComboOnAttackProp = serializedObject.FindProperty("NextComboOnAttack");
        _nextComboOnReleaseProp = serializedObject.FindProperty("NextComboOnRelease");
        _hitColliderWindowsProp = serializedObject.FindProperty("HitColliderWindows");
        _rangedHitWindowsProp = serializedObject.FindProperty("RangedHitWindows");

        _lastEditorTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
        _previewNeedsRebuild = true;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        CleanupPreview();
    }

    private void OnEditorUpdate()
    {
        if (!_isPlaying || _previewInstance == null || _currentClip == null)
        {
            _lastEditorTime = EditorApplication.timeSinceStartup;
            return;
        }

        double delta = EditorApplication.timeSinceStartup - _lastEditorTime;
        _lastEditorTime = EditorApplication.timeSinceStartup;
        _previewTime += delta;

        if (_previewTime > _currentClip.length)
            _previewTime = 0f;

        SampleAnimation(_previewTime);
        Repaint();
    }

    #endregion

    #region OnInspectorGUI

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawHeader();
        DrawBasicProperties();
        EditorGUILayout.Space(4);
        DrawHitColliderConfig();
        EditorGUILayout.Space(4);
        DrawRangedHitConfig();
        EditorGUILayout.Space(4);
        DrawTimelineSection();
        EditorGUILayout.Space(4);
        DrawAnimationPreview();
        EditorGUILayout.Space(4);
        DrawComboLinks();
        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(_config);
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField($"Combo Config: {_config.name}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"总计阶段时长: {GetTotalTime():F3}s", EditorStyles.miniLabel);
        EditorGUILayout.Space(3);
    }

    private void DrawBasicProperties()
    {
        _showBasicProperties = EditorGUILayout.Foldout(_showBasicProperties, "基本属性", true);
        if (!_showBasicProperties) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_animationClipNameProp, new GUIContent("动画剪辑名"));
        EditorGUILayout.PropertyField(_attackTypeProp, new GUIContent("攻击类型"));
        EditorGUILayout.PropertyField(_attackStrengthProp, new GUIContent("攻击力"));
        EditorGUI.indentLevel--;

        if (_currentClip != null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15 + 15);
            EditorGUILayout.LabelField($"动画信息: {_currentClip.name}  {_currentClip.length:F3}s  ({_currentClip.frameRate:F0}fps)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }

    #endregion

    #region HitCollider 配置

    private void DrawHitColliderConfig()
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("HitCollider 判定窗口", EditorStyles.boldLabel);

        if (_hitColliderWindowsProp == null)
        {
            EditorGUILayout.HelpBox("HitColliderWindows 属性未找到，请检查 ComboConfigSO 定义", MessageType.Error);
            return;
        }

        // 绘制数组属性（支持增删改）
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_hitColliderWindowsProp, new GUIContent("窗口列表"), true);
        EditorGUI.indentLevel--;

        // 如果数组不为空，显示每个窗口的摘要信息
        if (_config.HitColliderWindows != null && _config.HitColliderWindows.Length > 0)
        {
            EditorGUILayout.Space(2);
            for (int i = 0; i < _config.HitColliderWindows.Length; i++)
            {
                var w = _config.HitColliderWindows[i];
                float start = w.EnableTime;
                float end = w.EnableTime + w.Duration;
                EditorGUILayout.LabelField(
                    $"  窗口 [{i}]: 启用={start:F3}s → 禁用={end:F3}s (持续{w.Duration:F3}s)",
                    EditorStyles.miniLabel);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("未配置 HitCollider 窗口，攻击将无法造成伤害", MessageType.Warning);
        }
    }

    #endregion

    #region RangedHit 配置

    /// <summary>
    /// 远程攻击配置 — 独立于近战 HitCollider 窗口管理
    /// [解耦] 投射物生成时序由 RangedWeapon + RangedHitWindow 控制
    /// </summary>
    private void DrawRangedHitConfig()
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("远程攻击配置 (RangedHitWindow)", EditorStyles.boldLabel);

        if (_rangedHitWindowsProp == null)
        {
            EditorGUILayout.HelpBox("RangedHitWindows 属性未找到，请检查 ComboConfigSO 定义", MessageType.Error);
            return;
        }

        // 绘制数组属性（支持增删改）
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_rangedHitWindowsProp, new GUIContent("远程窗口列表"), true);
        EditorGUI.indentLevel--;

        // 如果数组不为空，显示每个窗口的摘要信息
        if (_config.RangedHitWindows != null && _config.RangedHitWindows.Length > 0)
        {
            EditorGUILayout.Space(2);
            for (int i = 0; i < _config.RangedHitWindows.Length; i++)
            {
                var w = _config.RangedHitWindows[i];
                EditorGUILayout.LabelField(
                    $"  远程窗口 [{i}]: 在 {w.EnableTime:F3}s 时生成, 武器索引={w.WeaponIndex}, 偏移={w.SpawnOffset}",
                    EditorStyles.miniLabel);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("未配置远程攻击窗口，远程攻击将不会触发", MessageType.Info);
        }
    }

    #endregion

    #region Timeline - 数据驱动

    private float GetTotalTime()
    {
        float total = 0f;
        for (int i = 0; i < PhaseCount; i++)
            total += _phaseProps[i].floatValue;
        // ⭐ 兼容 SO 直接赋值（拖拽时通过 prop 写入，但 ToString/序列化后可能不一致），
        // 优先使用 config 直接读也安全；但此处用 prop 确保显示值总是和序列化一致
        return total;
    }

    private float[] GetCumulativeBoundaries()
    {
        float[] bounds = new float[PhaseCount + 1];
        bounds[0] = 0f;
        for (int i = 0; i < PhaseCount; i++)
            bounds[i + 1] = bounds[i] + Mathf.Max(0f, _phaseProps[i].floatValue);
        return bounds;
    }

    private float GetTimelineTotal()
    {
        float phaseTotal = GetTotalTime();
        float clipLen = (_currentClip != null) ? _currentClip.length : 0f;
        return Mathf.Max(phaseTotal, clipLen, 1f);
    }

    private void DrawTimelineSection()
    {
        _showTimeline = EditorGUILayout.Foldout(_showTimeline, "攻击阶段时间线", true);
        if (!_showTimeline) return;

        float totalWidth = EditorGUIUtility.currentViewWidth - 30f;
        float leftMargin = TIMELINE_MARGIN_LEFT;

        Rect rulerRect = GUILayoutUtility.GetRect(totalWidth, TIMELINE_RULER_HEIGHT);
        Rect barRect = GUILayoutUtility.GetRect(totalWidth, TIMELINE_BAR_HEIGHT);

        DrawPhaseFieldsRow(barRect, leftMargin);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(leftMargin + 5);
        float total = GetTotalTime();
        EditorGUILayout.LabelField($"总计: {total:F3}s", EditorStyles.boldLabel);
        if (_currentClip != null)
        {
            GUI.color = Color.Lerp(Color.green, Color.red, Mathf.Abs(total - _currentClip.length) / _currentClip.length);
            EditorGUILayout.LabelField($"动画长度: {_currentClip.length:F3}s", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        DrawTimelineRuler(rulerRect, leftMargin);
        DrawTimelineBar(barRect, leftMargin);

        Rect hcBarRect = GUILayoutUtility.GetRect(totalWidth, HITCOLLIDER_BAR_HEIGHT + 4f);
        DrawHitColliderMiniBar(hcBarRect, leftMargin);

        HandleTimelineMouseEvents(barRect, leftMargin);
    }

    private void DrawPhaseFieldsRow(Rect barRect, float leftMargin)
    {
        float barWidth = barRect.width - leftMargin - TIMELINE_MARGIN_RIGHT;
        float barLeft = barRect.x + leftMargin;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(lineHeight + 2f));

        float cellW = barWidth / PhaseCount;
        for (int i = 0; i < PhaseCount; i++)
        {
            // ⭐ 使用 SerializedProperty 自动绘制，支持 Undo
            EditorGUI.PropertyField(
                new Rect(barLeft + i * cellW, rowRect.y + 1, cellW, lineHeight),
                _phaseProps[i], GUIContent.none);
        }
    }

    private void DrawTimelineRuler(Rect rect, float leftMargin)
    {
        Rect rulerRect = new Rect(rect.x, rect.y, rect.width, rect.height);
        EditorGUI.DrawRect(rulerRect, RulerBgColor);

        float timelineTotal = GetTimelineTotal();
        float barWidth = rulerRect.width - leftMargin - TIMELINE_MARGIN_RIGHT;
        float rulerLeft = rulerRect.x + leftMargin;

        Handles.BeginGUI();
        Handles.color = RulerTickColor;

        float tickInterval = timelineTotal <= 0.5f ? 0.05f
            : timelineTotal <= 1f ? 0.1f
            : timelineTotal <= 3f ? 0.25f
            : timelineTotal <= 5f ? 0.5f
            : 1f;

        for (float t = 0; t <= timelineTotal + 0.001f; t += tickInterval)
        {
            float x = rulerLeft + (t / timelineTotal) * barWidth;
            bool isMajor = Mathf.Abs(t % (tickInterval * 2)) < 0.001f
                           || Mathf.Abs(t % 1f) < 0.001f
                           || Mathf.Abs(t) < 0.001f;

            Handles.DrawLine(new Vector3(x, rulerRect.y + rulerRect.height),
                new Vector3(x, rulerRect.y + rulerRect.height - (isMajor ? 10f : 5f)));

            if (isMajor)
                GUI.Label(new Rect(x - 15, rulerRect.y + 2, 30, rulerRect.height - 2), $"{t:F1}s", Styles.RulerTickLabel);
        }
        Handles.EndGUI();
    }

    private void DrawTimelineBar(Rect rect, float leftMargin)
    {
        float totalTime = GetTotalTime();
        if (totalTime <= 0f) return;

        float timelineTotal = GetTimelineTotal();
        float barWidth = rect.width - leftMargin - TIMELINE_MARGIN_RIGHT;
        float barLeft = rect.x + leftMargin;
        float barTop = rect.y;
        float barHeight = rect.height;
        float[] boundaries = GetCumulativeBoundaries();

        // ⭐ 数据驱动：用 PhaseCount 替代硬编码 5
        for (int i = 0; i < PhaseCount; i++)
        {
            float phaseX = barLeft + (boundaries[i] / timelineTotal) * barWidth;
            float phaseW = Mathf.Max(1f, ((boundaries[i + 1] - boundaries[i]) / timelineTotal) * barWidth);

            EditorGUI.DrawRect(new Rect(phaseX, barTop, phaseW, barHeight), Phases[i].BgColor);
            EditorGUI.DrawRect(new Rect(phaseX, barTop, phaseW, barHeight * 0.7f), Phases[i].Color);

            if (phaseW > 15f)
            {
                var labelStyle = new GUIStyle(Styles.PhaseLabelOnBar) { fontSize = phaseW > 30f ? 9 : 8 };
                string label = phaseW > 30f ? Phases[i].NameCN : Phases[i].NameEN.Substring(0, 2);
                GUI.Label(new Rect(phaseX + 1, barTop + barHeight * 0.72f, phaseW - 2, barHeight * 0.25f), label, labelStyle);
            }
        }

        Handles.BeginGUI();
        // ⭐ 边界线：PhaseCount 个阶段产生 PhaseCount-1 条边界
        for (int i = 1; i < PhaseCount; i++)
        {
            float x = barLeft + (boundaries[i] / timelineTotal) * barWidth;
            bool hovered = _interactionMode == InteractionMode.DraggingBoundary && _dragBoundaryIndex == i;
            Handles.color = hovered ? BoundaryHoverColor : BoundaryLineColor;
            Handles.DrawLine(new Vector3(x, barTop), new Vector3(x, barTop + barHeight));
            EditorGUI.DrawRect(new Rect(x - DRAG_HANDLE_WIDTH * 0.5f, barTop + barHeight - 8f, DRAG_HANDLE_WIDTH, 8f),
                hovered ? BoundaryHoverColor : BoundaryLineColor);
        }

        if (_currentClip != null)
        {
            float px = barLeft + (float)(_previewTime / _currentClip.length) * barWidth;
            Handles.color = PlayheadColor;
            Handles.DrawLine(new Vector3(px, barTop), new Vector3(px, barTop + barHeight));
            Handles.DrawAAConvexPolygon(new Vector3[] {
                new Vector3(px, barTop + barHeight + 4f),
                new Vector3(px - 5f, barTop + barHeight + 12f),
                new Vector3(px + 5f, barTop + barHeight + 12f) });
        }
        Handles.EndGUI();

        // 拖拽光标区域
        for (int i = 1; i < PhaseCount; i++)
        {
            float x = barLeft + (boundaries[i] / timelineTotal) * barWidth;
            EditorGUIUtility.AddCursorRect(
                new Rect(x - DRAG_HANDLE_WIDTH * 0.5f - 3f, barTop, DRAG_HANDLE_WIDTH + 6f, barHeight),
                MouseCursor.ResizeHorizontal);
        }
    }

    /// <summary>
    /// 在阶段时间线下方绘制 HitCollider 窗口迷你条
    /// 每个窗口显示为一个亮黄色矩形条，清晰可见 EnableTime→Duration 的起止位置
    /// </summary>
    private void DrawHitColliderMiniBar(Rect rect, float leftMargin)
    {
        // 背景
        Rect bgRect = new Rect(rect.x, rect.y + 2f, rect.width, HITCOLLIDER_BAR_HEIGHT);
        EditorGUI.DrawRect(bgRect, HitColliderMiniBarBg);

        var windows = _config.HitColliderWindows;
        if (windows == null || windows.Length == 0)
        {
            EditorGUI.LabelField(bgRect, "未配置 HitCollider 窗口", Styles.PreviewHint);
            return;
        }

        float totalTime = GetTotalTime();
        if (totalTime <= 0f) return;

        float timelineTotal = GetTimelineTotal();
        float barWidth = rect.width - leftMargin - TIMELINE_MARGIN_RIGHT;
        float barLeft = rect.x + leftMargin;
        float barTop = bgRect.y;

        // 左侧"判定"标签
        GUI.Label(new Rect(rect.x + 2f, barTop, 50f, HITCOLLIDER_BAR_HEIGHT),
            "判定", new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 8,
                normal = { textColor = new Color(0.7f, 0.7f, 0.5f) }
            });

        for (int i = 0; i < windows.Length; i++)
        {
            float startX = barLeft + (windows[i].EnableTime / timelineTotal) * barWidth;
            float endX = barLeft + ((windows[i].EnableTime + windows[i].Duration) / timelineTotal) * barWidth;
            float w = Mathf.Max(2f, endX - startX);

            var windowRect = new Rect(startX, barTop + 1f, w, HITCOLLIDER_BAR_HEIGHT - 2f);
            EditorGUI.DrawRect(windowRect, HitColliderWindowColor);

            Handles.BeginGUI();
            Handles.color = HitColliderWindowOutline;
            Handles.DrawLine(new Vector3(startX, barTop), new Vector3(startX, barTop + HITCOLLIDER_BAR_HEIGHT));
            Handles.DrawLine(new Vector3(endX, barTop), new Vector3(endX, barTop + HITCOLLIDER_BAR_HEIGHT));
            Handles.EndGUI();

            if (w > 12f)
            {
                GUI.Label(windowRect, i.ToString(),
                    new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 7,
                        normal = { textColor = Color.black }
                    });
            }
        }
    }

    private void HandleTimelineMouseEvents(Rect barRect, float leftMargin)
    {
        Event e = Event.current;
        if (GetTotalTime() <= 0f) return;

        float timelineTotal = GetTimelineTotal();
        float barWidth = barRect.width - leftMargin - TIMELINE_MARGIN_RIGHT;
        float barLeft = barRect.x + leftMargin;
        float[] boundaries = GetCumulativeBoundaries();

        if (e.type == EventType.MouseDown && barRect.Contains(e.mousePosition))
        {
            // 检查是否点击到边界拖拽手柄
            for (int i = 1; i < PhaseCount; i++)
            {
                float bx = barLeft + (boundaries[i] / timelineTotal) * barWidth;
                if (Mathf.Abs(e.mousePosition.x - bx) <= DRAG_HANDLE_WIDTH)
                {
                    _interactionMode = InteractionMode.DraggingBoundary;
                    _dragBoundaryIndex = i;
                    _dragStartMouseX = e.mousePosition.x;
                    // ⭐ 保存各阶段当前值，便于计算增量
                    _dragStartPhaseValues = new float[PhaseCount];
                    for (int j = 0; j < PhaseCount; j++)
                        _dragStartPhaseValues[j] = _phaseProps[j].floatValue;
                    e.Use();
                    break;
                }
            }

            // 点击时间条跳转播放头
            if (_interactionMode == InteractionMode.None && _currentClip != null)
            {
                _previewTime = Mathf.Clamp01((e.mousePosition.x - barLeft) / barWidth) * _currentClip.length;
                SampleAnimation(_previewTime);
                _interactionMode = InteractionMode.DraggingScrubber;
                e.Use();
                Repaint();
            }
        }

        if (e.type == EventType.MouseDrag && _interactionMode == InteractionMode.DraggingBoundary)
        {
            float deltaPixels = e.mousePosition.x - _dragStartMouseX;
            float deltaTime = (deltaPixels / barWidth) * GetTotalTime();
            int boundaryIdx = _dragBoundaryIndex;

            // 计算拖拽前的累积边界
            float[] startBounds = new float[PhaseCount + 1];
            startBounds[0] = 0f;
            for (int j = 0; j < PhaseCount; j++)
                startBounds[j + 1] = startBounds[j] + _dragStartPhaseValues[j];

            float newBoundary = Mathf.Max(
                startBounds[boundaryIdx - 1] + 0.01f,
                Mathf.Min(startBounds[boundaryIdx + 1] - 0.01f, startBounds[boundaryIdx] + deltaTime));

            // ⭐ 通过 SerializedProperty 赋值 → 自动注册 Undo
            float newPhaseValue = newBoundary - startBounds[boundaryIdx - 1];
            _phaseProps[boundaryIdx - 1].floatValue = newPhaseValue;

            GUI.changed = true;
            e.Use();
            Repaint();
        }

        if (e.type == EventType.MouseDrag && _interactionMode == InteractionMode.DraggingScrubber && _currentClip != null)
        {
            _previewTime = Mathf.Clamp01((e.mousePosition.x - barLeft) / barWidth) * _currentClip.length;
            SampleAnimation(_previewTime);
            e.Use();
            Repaint();
        }

        if (e.type == EventType.MouseUp)
        {
            _interactionMode = InteractionMode.None;
            _dragBoundaryIndex = -1;
            _dragStartPhaseValues = null;
        }
    }

    #endregion

    #region Animation Preview

    private void DrawAnimationPreview()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("动画预览", EditorStyles.boldLabel);

        // Animation clip selector (drag from Project window)
        EditorGUI.BeginChangeCheck();
        AnimationClip newClip = EditorGUILayout.ObjectField(new GUIContent("动画剪辑", "拖入 AnimationClip 进行预览"),
            _config.PreviewAnimationClip, typeof(AnimationClip), false) as AnimationClip;
        if (EditorGUI.EndChangeCheck())
        {
            _config.PreviewAnimationClip = newClip;
            EditorUtility.SetDirty(_config);
            _previewNeedsRebuild = true;
        }

        // Preview model selector
        EditorGUILayout.BeginHorizontal();
        GameObject currentDisplayObj = _config.PreviewModel != null ? _config.PreviewModel : _scenePreviewModel;
        EditorGUI.BeginChangeCheck();
        GameObject newPreviewModel = EditorGUILayout.ObjectField(new GUIContent("预览模型", "拖入场景中的人物或 Prefab 资产，留空默认使用玩家模型"),
            currentDisplayObj, typeof(GameObject), true) as GameObject;
        if (EditorGUI.EndChangeCheck())
        {
            if (newPreviewModel == null) { _config.PreviewModel = null; _scenePreviewModel = null; }
            else if (AssetDatabase.Contains(newPreviewModel)) { _config.PreviewModel = newPreviewModel; _scenePreviewModel = null; EditorUtility.SetDirty(_config); }
            else { _config.PreviewModel = null; _scenePreviewModel = newPreviewModel; }
            _previewNeedsRebuild = true;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);

        DrawCameraControls();
        EditorGUILayout.Space(2);

        DrawPlaybackControls();
        EditorGUILayout.Space(2);
        DrawScrubber();
        EditorGUILayout.Space(2);
        DrawPreviewRender();
        EditorGUILayout.EndVertical();
    }

    private void DrawCameraControls()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("远裁剪面", GUILayout.Width(55));
        float newFarClip = EditorGUILayout.Slider(_cameraFarClipPlane, 1f, 100f, GUILayout.Height(16));
        if (Mathf.Abs(newFarClip - _cameraFarClipPlane) > 0.01f)
        {
            _cameraFarClipPlane = newFarClip;
            if (_previewRenderUtility != null)
                _previewRenderUtility.camera.farClipPlane = _cameraFarClipPlane;
            Repaint();
        }
        GUILayout.Space(10);
        EditorGUILayout.LabelField("缩放速度", GUILayout.Width(55));
        _cameraZoomSpeed = EditorGUILayout.Slider(_cameraZoomSpeed, 0.05f, 5f, GUILayout.Height(16));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPlaybackControls()
    {
        EditorGUILayout.BeginHorizontal();

        string playLabel = _isPlaying ? "⏸ 暂停" : "▶ 播放";
        if (GUILayout.Button(playLabel, GUILayout.Width(70), GUILayout.Height(20)))
        {
            _isPlaying = !_isPlaying;
            if (_isPlaying)
            {
                _lastEditorTime = EditorApplication.timeSinceStartup;
                EnsurePreviewBuilt();
            }
            Repaint();
        }

        if (GUILayout.Button("◀ 上一帧", GUILayout.Width(75), GUILayout.Height(20)) && _currentClip != null)
        {
            _isPlaying = false;
            _previewTime = Mathf.Max(0f, (float)(_previewTime - 1f / _currentClip.frameRate));
            SampleAnimation(_previewTime);
            Repaint();
        }

        if (GUILayout.Button("▶ 下一帧", GUILayout.Width(75), GUILayout.Height(20)) && _currentClip != null)
        {
            _isPlaying = false;
            _previewTime = Mathf.Min(_currentClip.length, (float)_previewTime + 1f / _currentClip.frameRate);
            SampleAnimation(_previewTime);
            Repaint();
        }

        if (GUILayout.Button("⏮ 重置", GUILayout.Width(60), GUILayout.Height(20)))
        {
            _isPlaying = false;
            _previewTime = 0;
            SampleAnimation(0);
            Repaint();
        }

        if (_currentClip != null)
        {
            float frameFloat = (float)(_previewTime * _currentClip.frameRate);
            EditorGUILayout.LabelField($"帧: {Mathf.RoundToInt(frameFloat)}/{Mathf.RoundToInt(_currentClip.length * _currentClip.frameRate)}", EditorStyles.miniLabel, GUILayout.Width(100));
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawScrubber()
    {
        if (_currentClip == null) return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{_previewTime:F3}s", EditorStyles.miniLabel, GUILayout.Width(55));

        float newTime = EditorGUILayout.Slider((float)_previewTime, 0f, _currentClip.length, GUILayout.Height(16));
        if (Mathf.Abs(newTime - (float)_previewTime) > 0.0001f)
        {
            _isPlaying = false;
            _previewTime = newTime;
            SampleAnimation(_previewTime);
            Repaint();
        }

        float totalTime = GetTotalTime();
        if (totalTime > 0f)
        {
            float normTime = (float)(_previewTime / _currentClip.length) * totalTime;
            EditorGUILayout.LabelField(GetPhaseAtTime(normTime), EditorStyles.miniLabel, GUILayout.Width(70));
        }
        EditorGUILayout.EndHorizontal();
    }

    private string GetPhaseAtTime(float time)
    {
        float[] bounds = GetCumulativeBoundaries();
        for (int i = PhaseCount - 1; i >= 0; i--)
        {
            if (time >= bounds[i] && time < bounds[i + 1])
                return $"{Phases[i].NameCN} {(time - bounds[i]) / (bounds[i + 1] - bounds[i] + 0.001f) * 100f:F0}%";
        }
        return "";
    }

    private void DrawPreviewRender()
    {
        // Must call EnsurePreviewBuilt BEFORE checking null, because it creates the render utility!
        EnsurePreviewBuilt();

        if (_previewRenderUtility == null)
        {
            EditorGUILayout.HelpBox("预览未初始化", MessageType.Info);
            return;
        }

        Rect previewRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40f, PREVIEW_HEIGHT);
        if (previewRect.width <= 0 || previewRect.height <= 0) return;

        _isHoveringPreview = previewRect.Contains(Event.current.mousePosition);
        HandleCameraMouseEvents(previewRect);

        if (_isHoveringPreview && Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(previewRect, PreviewBgColor);

        if (Event.current.type == EventType.Repaint)
        {
            UpdateCameraOrbit();
            _previewRenderUtility.camera.backgroundColor = PreviewBgColor;

            _previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);
            _previewRenderUtility.camera.Render();
            Texture renderedTexture = _previewRenderUtility.EndPreview();

            GUI.DrawTexture(previewRect, renderedTexture, ScaleMode.StretchToFill, false);
            DrawPreviewHints(previewRect);
        }
    }

    private void DrawPreviewHints(Rect previewRect)
    {
        string hintText = "🔄 左键拖拽旋转  |  🖱 中键拖拽平移  |  🔍 滚轮前后移动";
        Vector2 textSize = Styles.PreviewHint.CalcSize(new GUIContent(hintText));
        EditorGUI.DropShadowLabel(new Rect(previewRect.x + previewRect.width / 2 - textSize.x / 2, previewRect.y + previewRect.height - 18f, textSize.x, 16f), hintText, Styles.PreviewHint);
    }

    #endregion

    #region Camera Orbit

    private void HandleCameraMouseEvents(Rect previewRect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && _isHoveringPreview)
        {
            if (e.button == 0 || e.button == 2)
            {
                _interactionMode = InteractionMode.DraggingCamera;
                _cameraDragLastMouse = e.mousePosition;
                e.Use();
            }
        }

        if (e.type == EventType.MouseDrag && _interactionMode == InteractionMode.DraggingCamera && _isHoveringPreview)
        {
            Vector2 delta = e.mousePosition - _cameraDragLastMouse;
            _cameraDragLastMouse = e.mousePosition;

            if (e.button == 0)
            {
                _orbitYaw += delta.x * CAM_ROTATE_SPEED;
                _orbitPitch = Mathf.Clamp(_orbitPitch + delta.y * CAM_ROTATE_SPEED, -80f, 80f);
            }
            else if (e.button == 2)
            {
                float yawRad = _orbitYaw * Mathf.Deg2Rad;
                float pitchRad = _orbitPitch * Mathf.Deg2Rad;
                Vector3 forward = new Vector3(Mathf.Sin(yawRad) * Mathf.Cos(pitchRad), Mathf.Sin(pitchRad), Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)).normalized;
                Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
                Vector3 up = Vector3.Cross(right, forward).normalized;
                float panScale = _orbitDistance * CAM_PAN_SPEED;
                _orbitLookAt += (-right * delta.x + up * delta.y) * panScale;
            }

            Repaint();
            e.Use();
        }

        if (e.type == EventType.MouseUp && _interactionMode == InteractionMode.DraggingCamera)
        {
            _interactionMode = InteractionMode.None;
            e.Use();
        }

        if (e.type == EventType.ScrollWheel && _isHoveringPreview)
        {
            _orbitLookAt += (_previewRenderUtility.camera.transform.position - _orbitLookAt).normalized * e.delta.y * _cameraZoomSpeed * 0.02f;
            Repaint();
            e.Use();
        }
    }

    private void UpdateCameraOrbit()
    {
        if (_previewRenderUtility == null || _previewRenderUtility.camera == null) return;

        float yawRad = _orbitYaw * Mathf.Deg2Rad;
        float pitchRad = _orbitPitch * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            _orbitDistance * Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            _orbitDistance * Mathf.Sin(pitchRad),
            _orbitDistance * Mathf.Cos(yawRad) * Mathf.Cos(pitchRad));

        _previewRenderUtility.camera.transform.position = _orbitLookAt + offset;
        _previewRenderUtility.camera.transform.LookAt(_orbitLookAt);
    }

    #endregion

    #region Preview Setup / Sample

    private void EnsurePreviewBuilt()
    {
        if (!_previewNeedsRebuild && _previewInstance != null && _currentClip != null) return;

        AnimationClip clip = _config.PreviewAnimationClip;
        if (clip == null)
        {
            ClearPreview();
            return;
        }

        BuildPreview(clip);
        _previewNeedsRebuild = false;
    }

    private void BuildPreview(AnimationClip clip)
    {
        CleanupPreview();

        if (_previewRenderUtility == null)
        {
            _previewRenderUtility = new PreviewRenderUtility();
            _previewRenderUtility.camera.fieldOfView = _cameraFov;
            _previewRenderUtility.camera.nearClipPlane = 0.1f;
            _previewRenderUtility.camera.farClipPlane = _cameraFarClipPlane;
            _previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            _previewRenderUtility.camera.backgroundColor = PreviewBgColor;
            _previewRenderUtility.ambientColor = new Color(0.3f, 0.3f, 0.35f);
            _previewRenderUtility.lights[0].intensity = 0.8f;
            _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(30f, -30f, 0f);
            _previewRenderUtility.lights[1].intensity = 0.4f;
        }

        GameObject modelPrefab = null;

        if (_scenePreviewModel != null) { modelPrefab = _scenePreviewModel; }
        else if (_config.PreviewModel != null) { modelPrefab = _config.PreviewModel; }
        // ⭐ 使用可配置路径
        else { modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetModelPath()); }

        if (modelPrefab == null) { Debug.LogWarning("[ComboConfigSOInspector] 无法加载模型"); return; }

        // Resolve to a prefab asset that can be instantiated in the preview scene.
        GameObject prefabAsset = ResolvePrefabAsset(modelPrefab);

        // If the dragged object is a child, retry from the root ancestor
        if (prefabAsset == null && modelPrefab.transform.root != modelPrefab.transform)
            prefabAsset = ResolvePrefabAsset(modelPrefab.transform.root.gameObject);

        if (prefabAsset != null && AssetDatabase.Contains(prefabAsset))
        {
            // Use the preview render utility's internal scene for proper rendering
            _previewInstance = _previewRenderUtility.InstantiatePrefabInScene(prefabAsset);
        }
        else
        {
            // Last resort: instantiate and set layers so the preview camera's culling mask can see it
            _previewInstance = Object.Instantiate(modelPrefab);
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(_previewInstance, _previewRenderUtility.camera.gameObject.layer);
        }

        if (_previewInstance == null) { Debug.LogWarning("[ComboConfigSOInspector] 实例化模型失败"); return; }

        _previewInstance.transform.position = new Vector3(0f, -0.5f, 0f);
        _previewInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        _orbitYaw = 180f;
        _orbitPitch = 15f;
        _orbitDistance = CAM_DEFAULT_DISTANCE;
        _orbitLookAt = new Vector3(0f, CAM_DEFAULT_LOOKAT_Y, 0f);
        UpdateCameraOrbit();

        _currentClip = clip;

        if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();

        _previewTime = Mathf.Clamp((float)_previewTime, 0f, _currentClip.length);
        SampleAnimation(_previewTime);
    }

    /// <summary>
    /// Recursively resolve a scene GameObject to a prefab/FBX asset that can be
    /// instantiated in PreviewRenderUtility's internal scene.
    /// Resolution chain: asset → GetCorrespondingObjectFromSource → GetPrefabAssetPathOfNearestInstanceRoot
    /// → Mesh/FBX retro-lookup via sharedMesh.
    /// </summary>
    private static GameObject ResolvePrefabAsset(GameObject sceneObject)
    {
        if (sceneObject == null) return null;

        // Direct asset
        if (AssetDatabase.Contains(sceneObject))
            return sceneObject;

        // Prefab instance → source prefab
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(sceneObject);
        if (source != null && AssetDatabase.Contains(source))
            return source;

        // Nearest prefab root path
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sceneObject);
        if (!string.IsNullOrEmpty(prefabPath))
        {
            GameObject rootAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (rootAsset != null && AssetDatabase.Contains(rootAsset))
                return rootAsset;
        }

        // Last chance: retro-lookup via sharedMesh (handles FBX dragged into scene, non-prefab instances)
        Mesh sharedMesh = null;
        var skinnedRenderer = sceneObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
            sharedMesh = skinnedRenderer.sharedMesh;
        else
        {
            var meshFilter = sceneObject.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                sharedMesh = meshFilter.sharedMesh;
        }

        if (sharedMesh != null)
        {
            string meshAssetPath = AssetDatabase.GetAssetPath(sharedMesh);
            if (!string.IsNullOrEmpty(meshAssetPath))
            {
                GameObject rootAsset = AssetDatabase.LoadAssetAtPath<GameObject>(meshAssetPath);
                if (rootAsset != null && AssetDatabase.Contains(rootAsset))
                    return rootAsset;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively set the gameObject layer on all children.
    /// Used to ensure instantiated scene objects are visible to the preview camera.
    /// </summary>
    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void SampleAnimation(double time)
    {
        if (_previewInstance == null || _currentClip == null) return;
        AnimationMode.SampleAnimationClip(_previewInstance, _currentClip, Mathf.Clamp((float)time, 0f, _currentClip.length));
    }

    private void ClearPreview()
    {
        _currentClip = null;
        if (_previewInstance != null) { Object.DestroyImmediate(_previewInstance); _previewInstance = null; }
    }

    private void CleanupPreview()
    {
        ClearPreview();
        if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
        if (_previewRenderUtility != null) { _previewRenderUtility.Cleanup(); _previewRenderUtility = null; }
    }

    #endregion

    #region Combo Links

    private void DrawComboLinks()
    {
        _showComboLinks = EditorGUILayout.Foldout(_showComboLinks, "连击链接", true);
        if (!_showComboLinks) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_nextComboOnAttackProp, new GUIContent("攻击后连击 (NextComboOnAttack)"));
        EditorGUILayout.PropertyField(_nextComboOnReleaseProp, new GUIContent("释放后连击 (NextComboOnRelease)"));
        EditorGUI.indentLevel--;

        if (_config.NextComboOnAttack != null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15 + 15);
            var n = _config.NextComboOnAttack;
            EditorGUILayout.LabelField($"→ {n.name}  ({n.ChargeTime:F2}+{n.WindupTime:F2}+{n.AttackDuration:F2}+{n.RecoveryTime:F2}+{n.ExitTime:F2})", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }

    #endregion
}
