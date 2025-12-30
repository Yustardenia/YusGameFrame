#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(YusWorkflowBlackboardEntry))]
public sealed class YusWorkflowBlackboardEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using (new EditorGUI.PropertyScope(position, label, property))
        {
            position = EditorGUI.PrefixLabel(position, label);

            var keyProp = property.FindPropertyRelative(nameof(YusWorkflowBlackboardEntry.Key));
            var kindProp = property.FindPropertyRelative(nameof(YusWorkflowBlackboardEntry.Kind));
            var intProp = property.FindPropertyRelative(nameof(YusWorkflowBlackboardEntry.IntValue));
            var floatProp = property.FindPropertyRelative(nameof(YusWorkflowBlackboardEntry.FloatValue));
            var boolProp = property.FindPropertyRelative(nameof(YusWorkflowBlackboardEntry.BoolValue));
            var stringProp = property.FindPropertyRelative(nameof(YusWorkflowBlackboardEntry.StringValue));

            const float gap = 6f;
            float line = EditorGUIUtility.singleLineHeight;

            var keyRect = position;
            var kindRect = position;
            var valueRect = position;

            float keyWidth = Mathf.Clamp(position.width * 0.48f, 120f, 260f);
            float kindWidth = Mathf.Clamp(position.width * 0.22f, 70f, 120f);

            keyRect.width = keyWidth;
            kindRect.x += keyWidth + gap;
            kindRect.width = kindWidth;
            valueRect.x += keyWidth + gap + kindWidth + gap;
            valueRect.width = Mathf.Max(40f, position.width - (keyWidth + gap + kindWidth + gap));

            keyRect.height = kindRect.height = valueRect.height = line;

            EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
            EditorGUI.PropertyField(kindRect, kindProp, GUIContent.none);

            var kind = (YusWorkflowBlackboardEntry.ValueKind)kindProp.enumValueIndex;
            switch (kind)
            {
                case YusWorkflowBlackboardEntry.ValueKind.Int:
                    EditorGUI.PropertyField(valueRect, intProp, GUIContent.none);
                    break;
                case YusWorkflowBlackboardEntry.ValueKind.Float:
                    EditorGUI.PropertyField(valueRect, floatProp, GUIContent.none);
                    break;
                case YusWorkflowBlackboardEntry.ValueKind.Bool:
                    EditorGUI.PropertyField(valueRect, boolProp, GUIContent.none);
                    break;
                case YusWorkflowBlackboardEntry.ValueKind.String:
                    EditorGUI.PropertyField(valueRect, stringProp, GUIContent.none);
                    break;
                default:
                    EditorGUI.LabelField(valueRect, "Unsupported");
                    break;
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
#endif

