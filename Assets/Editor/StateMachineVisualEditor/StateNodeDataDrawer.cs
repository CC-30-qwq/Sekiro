using UnityEditor;
using UnityEngine;

namespace Sekiro.BehaviourMachine.Editor
{
    /// <summary>
    /// StateNodeData 的自定义 PropertyDrawer
    /// 在 Inspector 列表中将 "Element X" 替换为状态名称
    /// </summary>
    [CustomPropertyDrawer(typeof(StateNodeData))]
    public class StateNodeDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var stateProp = property.FindPropertyRelative("state");
            label.text = stateProp.enumDisplayNames[stateProp.enumValueIndex];
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
