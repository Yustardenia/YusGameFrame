using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SimpleValueViewer : EditorWindow
{
    private Vector2 scrollPos;
    private string savePath;

    private class SimpleDataCache
    {
        public string key;
        public string type;
        public object value;
        public bool isDirty;
    }

    private readonly List<SimpleDataCache> dataList = new List<SimpleDataCache>();

    [MenuItem("Tools/Yus Data/A.3 Simple Value Viewer")]
    public static void ShowWindow()
    {
        GetWindow<SimpleValueViewer>("Simple Value Viewer");
    }

    private void OnEnable()
    {
        savePath = SimpleSingleValueSaver.SAVE_PATH;
        RefreshData();
    }

    private void OnGUI()
    {
        GUILayout.Label("YusSimple (Single Value Saves)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Height(30)))
        {
            RefreshData();
        }
        if (GUILayout.Button("Open Folder", GUILayout.Height(30)))
        {
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            EditorUtility.RevealInFinder(savePath);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (dataList.Count == 0)
        {
            GUILayout.Label("No data.");
            return;
        }

        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("Key", GUILayout.Width(180));
        GUILayout.Label("Type", GUILayout.Width(220));
        GUILayout.Label("Value", GUILayout.ExpandWidth(true));
        GUILayout.Label("Action", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < dataList.Count; i++)
        {
            DrawItem(dataList[i]);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawItem(SimpleDataCache item)
    {
        EditorGUILayout.BeginHorizontal("box");

        GUILayout.Label(item.key, GUILayout.Width(180));
        GUILayout.Label(ShortTypeName(item.type), EditorStyles.miniLabel, GUILayout.Width(220));

        object oldVal = item.value;
        object newVal = oldVal;

        switch (item.type)
        {
            case "int":
                newVal = EditorGUILayout.IntField(oldVal is int i ? i : 0);
                break;
            case "float":
                newVal = EditorGUILayout.FloatField(oldVal is float f ? f : 0f);
                break;
            case "bool":
                newVal = EditorGUILayout.Toggle(oldVal is bool b && b);
                break;
            case "string":
                newVal = EditorGUILayout.TextField(oldVal as string ?? "");
                break;
            default:
                EditorGUILayout.SelectableLabel(FormatValue(oldVal), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                break;
        }

        if (!Equals(oldVal, newVal))
        {
            item.value = newVal;
            item.isDirty = true;
        }

        GUI.color = item.isDirty ? Color.green : Color.white;
        if (GUILayout.Button(item.isDirty ? "Save" : "Del", GUILayout.Width(60)))
        {
            if (item.isDirty)
            {
                SaveItemToDisk(item);
                item.isDirty = false;
                GUI.FocusControl(null);
                ShowNotification(new GUIContent("Saved"));
            }
            else
            {
                if (EditorUtility.DisplayDialog("Delete", $"Delete '{item.key}'?", "Delete", "Cancel"))
                {
                    SimpleSingleValueSaver.Delete(item.key);
                    RefreshData();
                    GUIUtility.ExitGUI();
                }
            }
        }
        GUI.color = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void RefreshData()
    {
        dataList.Clear();
        if (!Directory.Exists(savePath)) return;

        string[] files = Directory.GetFiles(savePath, "*.yus");
        foreach (var file in files)
        {
            if (!SimpleSingleValueSaver.TryReadFile(file, out var type, out var val)) continue;

            dataList.Add(new SimpleDataCache
            {
                key = Path.GetFileNameWithoutExtension(file),
                type = type,
                value = val,
                isDirty = false
            });
        }
    }

    private static void SaveItemToDisk(SimpleDataCache item)
    {
        switch (item.type)
        {
            case "int":
                SimpleSingleValueSaver.Save(item.key, (int)item.value);
                return;
            case "float":
                SimpleSingleValueSaver.Save(item.key, (float)item.value);
                return;
            case "bool":
                SimpleSingleValueSaver.Save(item.key, (bool)item.value);
                return;
            case "string":
                SimpleSingleValueSaver.Save(item.key, (string)item.value);
                return;
            default:
                EditorUtility.DisplayDialog("Unsupported", $"Editing/saving this type is not supported here: {item.type}", "OK");
                return;
        }
    }

    private static string ShortTypeName(string typeStr)
    {
        if (string.IsNullOrEmpty(typeStr)) return "null";
        if (typeStr == "int" || typeStr == "float" || typeStr == "bool" || typeStr == "string") return typeStr;

        int comma = typeStr.IndexOf(',');
        string name = comma >= 0 ? typeStr.Substring(0, comma) : typeStr;

        int lastDot = name.LastIndexOf('.');
        if (lastDot >= 0) name = name.Substring(lastDot + 1);

        return name;
    }

    private static string FormatValue(object val)
    {
        if (val == null) return "null";
        if (val is string s) return s;

        try
        {
            string json = EditorJsonUtility.ToJson(val, true);
            if (!string.IsNullOrEmpty(json) && json != "{}") return json;
        }
        catch { }

        return val.ToString();
    }
}
