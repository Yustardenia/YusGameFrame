#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

// 数据结构：单条着色规则
[System.Serializable]
public class FolderColorRule
{
    public string folderName;
    public Color color;

    public FolderColorRule(string name, Color col)
    {
        folderName = name;
        color = col;
    }
}

// 数据容器：用于序列化存储
[System.Serializable]
public class FolderColorData
{
    public List<FolderColorRule> rules = new List<FolderColorRule>();
}

// -----------------------------------------------------------
// 1. 配置窗口 (The Config Window)
// -----------------------------------------------------------
public class FolderColorizerWindow : EditorWindow
{
    private Vector2 scrollPos;
    private static FolderColorData data; // 运行时缓存的数据
    private const string PREFS_KEY = "FolderColorizer_Config_JSON";

    [MenuItem("Tools/🎨 文件夹染色配置 (Folder Colorizer)", false, 0)]
    public static void ShowWindow()
    {
        GetWindow<FolderColorizerWindow>("文件夹染色");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void OnDisable()
    {
        SaveData();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("拖拽文件夹到此处可快速添加。\n颜色 Alpha 值建议设置在 30-80 之间。", MessageType.Info);
        GUILayout.Space(5);

        // --- 拖拽处理 ---
        Rect dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "拖拽文件夹到这里 👇", EditorStyles.centeredGreyMiniLabel);
        HandleDragDrop(dropRect);

        GUILayout.Space(10);

        // --- 列表绘制 ---
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (data.rules.Count == 0)
        {
            GUILayout.Label("暂无配置，请添加或拖入文件夹。", EditorStyles.centeredGreyMiniLabel);
        }

        for (int i = 0; i < data.rules.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("box");
            
            // 名字输入框
            data.rules[i].folderName = EditorGUILayout.TextField(data.rules[i].folderName, GUILayout.Width(150));
            
            // 颜色选择器
            data.rules[i].color = EditorGUILayout.ColorField(data.rules[i].color);

            // 删除按钮
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                data.rules.RemoveAt(i);
                SaveData(); // 立即保存并刷新
                GUIUtility.ExitGUI(); // 防止报错
            }
            
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        // --- 底部按钮 ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 添加新规则", GUILayout.Height(30)))
        {
            data.rules.Add(new FolderColorRule("NewFolder", new Color(0.5f, 0.5f, 0.5f, 0.2f)));
            scrollPos.y = float.MaxValue; // 滚到底部
        }

        if (GUILayout.Button("重置默认", GUILayout.Width(80), GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("重置", "确定要恢复默认的颜色配置吗？", "确定", "取消"))
            {
                ResetToDefaults();
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);

        // 监听变化，如果有改动则实时刷新 Project 窗口
        if (GUI.changed)
        {
            SaveData();
        }
    }

    private void HandleDragDrop(Rect rect)
    {
        Event evt = Event.current;
        if (rect.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                bool added = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        string folderName = System.IO.Path.GetFileName(path);
                        // 查重
                        if (!data.rules.Any(r => r.folderName == folderName))
                        {
                            // 随机分配一个浅色
                            Color randomCol = Color.HSVToRGB(Random.value, 0.6f, 1f);
                            randomCol.a = 0.25f;
                            data.rules.Add(new FolderColorRule(folderName, randomCol));
                            added = true;
                        }
                    }
                }
                if (added) SaveData();
                evt.Use();
            }
        }
    }

    // --- 数据持久化逻辑 ---
    public static void LoadData()
    {
        if (EditorPrefs.HasKey(PREFS_KEY))
        {
            string json = EditorPrefs.GetString(PREFS_KEY);
            data = JsonUtility.FromJson<FolderColorData>(json);
        }
        
        if (data == null || data.rules == null)
        {
            ResetToDefaults();
        }
        
        // 同步数据给绘制器
        FolderColorizer.UpdateRules(data.rules);
        if (data == null) data = new FolderColorData(); 
        if (data.rules == null) data.rules = new List<FolderColorRule>();

        FolderColorizer.UpdateRules(data.rules);
    }

    public static void SaveData()
    {
        if (data == null) return;
        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(PREFS_KEY, json);
        
        // 通知 Project 窗口重绘
        FolderColorizer.UpdateRules(data.rules);
        EditorApplication.RepaintProjectWindow();
    }

    private static void ResetToDefaults()
    {
        data = new FolderColorData();
        data.rules.Add(new FolderColorRule("Scripts", new Color(1f, 0.3f, 0.3f, 0.25f)));
        data.rules.Add(new FolderColorRule("Scenes", new Color(0.3f, 0.8f, 0.3f, 0.25f)));
        data.rules.Add(new FolderColorRule("Prefabs", new Color(0.8f, 0.4f, 0.8f, 0.25f)));
        data.rules.Add(new FolderColorRule("Resources", new Color(0.2f, 0.6f, 1f, 0.25f)));
        data.rules.Add(new FolderColorRule("Editor", new Color(0.5f, 0.5f, 0.5f, 0.25f)));
        SaveData();
    }
}

// -----------------------------------------------------------
// 2. 绘制逻辑 (The Drawer)
// -----------------------------------------------------------
[InitializeOnLoad]
public static class FolderColorizer
{
    // 缓存字典，用于高频绘制时的快速查找 O(1)
    private static Dictionary<string, Color> colorDict = new Dictionary<string, Color>();

    static FolderColorizer()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowGUI;
        // 启动时加载一次数据
        FolderColorizerWindow.LoadData(); 
    }

    // 供 Window 调用的刷新方法
    public static void UpdateRules(List<FolderColorRule> rules)
    {
        colorDict.Clear();
        foreach (var rule in rules)
        {
            if (!string.IsNullOrEmpty(rule.folderName) && !colorDict.ContainsKey(rule.folderName))
            {
                colorDict.Add(rule.folderName, rule.color);
            }
        }
    }

    private static void OnProjectWindowGUI(string guid, Rect selectionRect)
    {
        // 1. 安全检查：如果字典没初始化，或者矩形太小（不可见），直接跳过
        if (colorDict == null || colorDict.Count == 0) return;
        if (selectionRect.width <= 1 || selectionRect.height <= 1) return;

        try 
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;

            string name = System.IO.Path.GetFileName(path);
            
            if (colorDict.TryGetValue(name, out Color color))
            {
                // 再次确认有效性，防止 AssetDatabase 在某些时刻返回错误
                if (!AssetDatabase.IsValidFolder(path)) return;

                DrawColor(selectionRect, color);
            }
        }
        catch (System.Exception)
        {
            // 2. 吞掉异常：
            // 在 GUI 绘制中，为了防止把 Unity 编辑器搞崩出现 TLS 错误，
            // 宁可不画颜色，也不要抛出异常打断绘制管线。
            return;
        }
    }

    private static void DrawColor(Rect rect, Color color)
    {
        var originalColor = GUI.color;
        GUI.color = color;

        // 判断视图模式
        if (rect.height > 20) 
        {
            // 图标模式 (Grid)
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
        }
        else
        {
            // 列表模式 (List) - 绘制圆角标签风格
            // 这里我们做一个微调，让它不遮挡左边的箭头
            Rect labelRect = new Rect(rect.x + 14, rect.y, rect.width - 14, rect.height);
            GUI.DrawTexture(labelRect, Texture2D.whiteTexture);
        }

        GUI.color = originalColor;
    }
}
#endif