using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class SimpleValueViewer : EditorWindow
{
    private Vector2 scrollPos;
    private string savePath;
    
    // 缓存读取到的数据，避免每帧都读文件
    private class SimpleDataCache
    {
        public string key;
        public string type;
        public object value;
        public bool isDirty; // 是否被修改过
    }
    
    private List<SimpleDataCache> dataList = new List<SimpleDataCache>();

    [MenuItem("Tools/Yus Data/简单值查看器 (Simple Viewer)")]
    public static void ShowWindow()
    {
        GetWindow<SimpleValueViewer>("简单值管理");
    }

    private void OnEnable()
    {
        // 获取运行时脚本定义的路径
        savePath = SimpleSingleValueSaver.SAVE_PATH;
        RefreshData();
    }

    private void OnGUI()
    {
        GUILayout.Label("简单二进制数据管理器 (YusSimple)", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新列表", GUILayout.Height(30)))
        {
            RefreshData();
        }
        if (GUILayout.Button("打开存档文件夹", GUILayout.Height(30)))
        {
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            EditorUtility.RevealInFinder(savePath);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        
        if (dataList.Count == 0)
        {
            GUILayout.Label("暂无存档数据");
            return;
        }

        // 表头
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("Key (文件名)", GUILayout.Width(150));
        GUILayout.Label("Type", GUILayout.Width(50));
        GUILayout.Label("Value (可编辑)", GUILayout.ExpandWidth(true));
        GUILayout.Label("操作", GUILayout.Width(60));
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

        // 1. Key
        GUILayout.Label(item.key, GUILayout.Width(150));
        
        // 2. Type
        GUILayout.Label(item.type, EditorStyles.miniLabel, GUILayout.Width(50));

        // 3. Value (根据类型显示不同的控件)
        object oldVal = item.value;
        object newVal = oldVal;

        switch (item.type)
        {
            case "int":
                newVal = EditorGUILayout.IntField((int)oldVal);
                break;
            case "float":
                newVal = EditorGUILayout.FloatField((float)oldVal);
                break;
            case "bool":
                newVal = EditorGUILayout.Toggle((bool)oldVal);
                break;
            case "string":
                newVal = EditorGUILayout.TextField((string)oldVal);
                break;
            default:
                GUILayout.Label(oldVal.ToString());
                break;
        }

        // 检测数值是否改变
        if (!object.Equals(oldVal, newVal))
        {
            item.value = newVal;
            item.isDirty = true;
        }

        // 4. Save/Delete Buttons
        GUI.color = item.isDirty ? Color.green : Color.white;
        if (GUILayout.Button(item.isDirty ? "保存" : "Del", GUILayout.Width(60)))
        {
            if (item.isDirty)
            {
                SaveItemToDisk(item);
                item.isDirty = false;
                GUI.FocusControl(null); // 取消焦点，防止输入框未提交
                ShowNotification(new GUIContent("保存成功"));
            }
            else
            {
                if (EditorUtility.DisplayDialog("删除", $"确定要删除 {item.key} 吗?", "删除", "取消"))
                {
                    SimpleSingleValueSaver.Delete(item.key);
                    RefreshData();
                    GUIUtility.ExitGUI(); // 防止删除后重绘报错
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
            try
            {
                using (var br = new BinaryReader(File.Open(file, FileMode.Open)))
                {
                    // 简单校验 Header
                    byte[] magic = br.ReadBytes(4);
                    if (Encoding.UTF8.GetString(magic) != "YUSV") continue; // 跳过非单值文件

                    int version = br.ReadInt32();
                    string type = ReadString(br);
                    object val = ReadValue(br, type);

                    dataList.Add(new SimpleDataCache
                    {
                        key = Path.GetFileNameWithoutExtension(file),
                        type = type,
                        value = val,
                        isDirty = false
                    });
                }
            }
            catch
            {
                // 忽略读取错误的文件
            }
        }
    }

    // 复用一下读取逻辑 (为了Editor独立性，这里稍微冗余一点也没事)
    private string ReadString(BinaryReader br)
    {
        int len = br.ReadInt32();
        return Encoding.UTF8.GetString(br.ReadBytes(len));
    }

    private object ReadValue(BinaryReader br, string type)
    {
        switch (type)
        {
            case "int": return br.ReadInt32();
            case "float": return br.ReadSingle();
            case "bool": return br.ReadByte() != 0;
            case "string": return ReadString(br);
            default: return "Unknown";
        }
    }

    private void SaveItemToDisk(SimpleDataCache item)
    {
        // 利用反射调用运行时脚本的保存逻辑，或者直接调用 Save 方法
        // 这里为了方便，直接根据类型分发调用泛型 Save
        switch (item.type)
        {
            case "int": SimpleSingleValueSaver.Save(item.key, (int)item.value); break;
            case "float": SimpleSingleValueSaver.Save(item.key, (float)item.value); break;
            case "bool": SimpleSingleValueSaver.Save(item.key, (bool)item.value); break;
            case "string": SimpleSingleValueSaver.Save(item.key, (string)item.value); break;
        }
    }
}