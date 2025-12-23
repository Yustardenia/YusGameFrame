#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomPropertyDrawer(typeof(SceneSelectorAttribute))]
public class SceneSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 获取 Build Settings 里的所有场景名称
        var scenes = EditorBuildSettings.scenes
            .Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))
            .ToArray();

        if (scenes.Length == 0)
        {
            EditorGUI.LabelField(position, label.text, "Build Settings为空");
            return;
        }

        // 1. 如果字段是 string 类型
        if (property.propertyType == SerializedPropertyType.String)
        {
            int index = Mathf.Max(0, System.Array.IndexOf(scenes, property.stringValue));
            index = EditorGUI.Popup(position, label.text, index, scenes);
            property.stringValue = scenes[index];
        }
        // 2. 如果字段是 int 类型 (Build Index)
        else if (property.propertyType == SerializedPropertyType.Integer)
        {
            int index = property.intValue;
            // 防止越界
            if (index < 0 || index >= scenes.Length) index = 0;
            
            index = EditorGUI.Popup(position, label.text, index, scenes);
            property.intValue = index;
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "仅支持 string 或 int");
        }
    }
}
#endif