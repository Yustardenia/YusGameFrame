using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(YusEventNameAttribute))]
public class YusEventNameDrawer : PropertyDrawer
{
    private const string CustomLabel = "<Custom>";

    private static string[] _cachedEventNames;
    private static GUIContent[] _cachedOptions;

    private static void EnsureCache()
    {
        if (_cachedOptions != null) return;

        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
            })
            .FirstOrDefault(t => t.Name == "YusEvents");

        var names = new List<string>();
        if (type != null)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                if (field.FieldType != typeof(string)) continue;
                if (!field.IsLiteral || field.IsInitOnly) continue;
                if (field.GetRawConstantValue() is string value && !string.IsNullOrEmpty(value))
                {
                    names.Add(value);
                }
            }
        }

        names = names.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();

        _cachedEventNames = names.ToArray();
        _cachedOptions = new GUIContent[_cachedEventNames.Length + 1];
        _cachedOptions[0] = new GUIContent(CustomLabel);
        for (var i = 0; i < _cachedEventNames.Length; i++)
        {
            _cachedOptions[i + 1] = new GUIContent(_cachedEventNames[i]);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        EnsureCache();

        if (property.propertyType != SerializedPropertyType.String)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        var allowCustom = ((YusEventNameAttribute)attribute).AllowCustom;
        if (!allowCustom) return EditorGUIUtility.singleLineHeight;

        if (string.IsNullOrEmpty(property.stringValue)) return EditorGUIUtility.singleLineHeight;

        var isKnown = Array.IndexOf(_cachedEventNames, property.stringValue) >= 0;
        return isKnown ? EditorGUIUtility.singleLineHeight : (EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureCache();

        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        var allowCustom = ((YusEventNameAttribute)attribute).AllowCustom;

        EditorGUI.BeginProperty(position, label, property);

        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var isKnown = Array.IndexOf(_cachedEventNames, property.stringValue) >= 0;
        var currentIndex = isKnown ? (Array.IndexOf(_cachedEventNames, property.stringValue) + 1) : 0;

        using (new EditorGUI.DisabledScope(_cachedEventNames.Length == 0))
        {
            var newIndex = EditorGUI.Popup(line, label, currentIndex, _cachedOptions);
            if (newIndex != currentIndex)
            {
                if (newIndex == 0)
                {
                    if (!allowCustom)
                    {
                        property.stringValue = _cachedEventNames.Length > 0 ? _cachedEventNames[0] : "";
                    }
                }
                else
                {
                    property.stringValue = _cachedEventNames[newIndex - 1];
                }
            }
        }

        if (allowCustom && !isKnown && !string.IsNullOrEmpty(property.stringValue))
        {
            var line2 = new Rect(position.x, line.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
            property.stringValue = EditorGUI.TextField(line2, GUIContent.none, property.stringValue);
        }

        EditorGUI.EndProperty();
    }
}
