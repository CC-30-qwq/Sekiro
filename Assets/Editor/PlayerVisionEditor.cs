using UnityEditor;
using UnityEngine;

/// <summary>
/// PlayerVision 自定义 Inspector，运行时预览 VisibleTargets 列表
/// </summary>
[CustomEditor(typeof(PlayerVision))]
public class PlayerVisionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认 Serialized Fields
        DrawDefaultInspector();

        PlayerVision vision = (PlayerVision)target;

        // 只在 Play Mode 下显示运行时数据
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play Mode 后可实时查看可见目标列表", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== Runtime Debug ===", EditorStyles.boldLabel);

        // 显示最优目标
        EditorGUILayout.BeginVertical("box");
        {
            EditorGUILayout.LabelField("Best Target", vision.BestTarget != null
                ? $"{vision.BestTarget.name} (Score: {GetBestScore(vision):F2})"
                : "None");
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 显示所有可见目标（带评分排序）
        var targets = vision.VisibleTargets;
        if (targets == null || targets.Count == 0)
        {
            EditorGUILayout.LabelField("Visible Targets: 0");
            return;
        }

        EditorGUILayout.LabelField($"Visible Targets: {targets.Count}", EditorStyles.boldLabel);

        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            string icon = i == 0 ? "★" : "•";
            float progress = Mathf.Clamp01(1f - target.Score / 10f); // 归一化进度条

            EditorGUILayout.BeginVertical("box");
            {
                // 目标名称 + 评分
                EditorGUILayout.LabelField(
                    $"{icon} [{i}] {target.Root?.name ?? "null"}",
                    i == 0 ? EditorStyles.boldLabel : EditorStyles.label);

                // 评分进度条（直观显示优先级）
                Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.ProgressBar(rect, progress, $"Score: {target.Score:F2}");

                // 命中点信息
                EditorGUILayout.LabelField(
                    $"HitPoint: {target.HitPoint?.name ?? "null"}",
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(2);
        }
    }

    private float GetBestScore(PlayerVision vision)
    {
        var targets = vision.VisibleTargets;
        if (targets != null && targets.Count > 0)
            return targets[0].Score;
        return -1f;
    }
}
