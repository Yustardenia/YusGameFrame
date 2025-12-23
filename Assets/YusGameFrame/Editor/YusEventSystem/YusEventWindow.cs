using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public class YusEventWindow : EditorWindow
{
    private const string EVENT_FILE_PATH = "Assets/YusGameFrame/YusEventSystem/YusEvents.cs"; // 请确保路径正确

    // 状态
    private Vector2 scrollLeft;
    private Vector2 scrollRight;
    private string newEventName = "ON_NEW_EVENT";
    private int selectedTab = 0; // 0=管理, 1=调试
    private string searchFilter = ""; // 搜索过滤

    // 缓存
    private List<string> existingEvents = new List<string>();

    [MenuItem(YusGameFrameEditorMenu.Root + "Systems/Event/事件中心 (Event Center)")]
    public static void ShowWindow()
    {
        GetWindow<YusEventWindow>("事件中心");
    }

    private void OnEnable()
    {
        LoadEventFile();
    }

    private void OnGUI()
    {
        GUILayout.Label("📡 Yus 事件中心", EditorStyles.boldLabel);
        
        selectedTab = GUILayout.Toolbar(selectedTab, new string[] { "📝 事件管理 (代码生成)", "🔍 运行时调试" });

        if (selectedTab == 0) DrawManagementTab();
        else DrawDebugTab();
    }

    // --- Tab 1: 事件管理 ---
    private void DrawManagementTab()
    {
        EditorGUILayout.Space();
        
        // 1. 新增区域
        EditorGUILayout.BeginHorizontal("box");
        newEventName = EditorGUILayout.TextField("新事件名:", newEventName);
        if (GUILayout.Button("➕ 添加并生成", GUILayout.Width(100)))
        {
            AddEventToFile(newEventName);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label($"现有事件 ({existingEvents.Count})", EditorStyles.boldLabel);

        // 2. 列表区域
        scrollLeft = EditorGUILayout.BeginScrollView(scrollLeft, "box");
        foreach (var evt in existingEvents)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(evt, GUILayout.Height(20));
            if (GUILayout.Button("复制", GUILayout.Width(50)))
            {
                EditorGUIUtility.systemCopyBuffer = $"YusEvents.{evt}";
                ShowNotification(new GUIContent("已复制代码"));
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("🔄 刷新文件读取"))
        {
            LoadEventFile();
        }
    }

    // --- Tab 2: 运行时调试 ---
    private void DrawDebugTab()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请先运行游戏以查看实时数据。", MessageType.Info);
            return;
        }

        var manager = YusEventManager.Instance;
        if (manager == null) return;

        var table = manager.GetEventTable();

        EditorGUILayout.BeginHorizontal();

        // 左栏：订阅者状态
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.6f));
        
        // 搜索栏
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"🔥 活跃事件 ({table.Count})", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));
        if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20))) searchFilter = "";
        EditorGUILayout.EndHorizontal();

        scrollLeft = EditorGUILayout.BeginScrollView(scrollLeft);

        foreach (var kvp in table)
        {
            if (kvp.Value == null) continue;
            
            // 过滤逻辑
            if (!string.IsNullOrEmpty(searchFilter) && 
                !kvp.Key.ToLower().Contains(searchFilter.ToLower())) 
            {
                continue;
            }

            // 获取调用列表 (谁订阅了)
            var invocationList = kvp.Value.GetInvocationList();
            
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(kvp.Key, EditorStyles.boldLabel);
            GUILayout.Label($"{invocationList.Length} 监听者", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            foreach (var d in invocationList)
            {
                string targetName = d.Target != null ? d.Target.ToString() : "Static";
                string methodName = d.Method.Name;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  ↳ {targetName} . {methodName}()");
                
                // Ping 按钮
                if (d.Target is MonoBehaviour mb)
                {
                    if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        EditorGUIUtility.PingObject(mb.gameObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // 右栏：广播历史
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📢 广播历史 (最新50条)", EditorStyles.boldLabel);
        scrollRight = EditorGUILayout.BeginScrollView(scrollRight);
        
        // 倒序显示
        for (int i = manager.history.Count - 1; i >= 0; i--)
        {
            var record = manager.history[i];
            EditorGUILayout.BeginVertical("helpbox");
            GUILayout.Label($"[{record.time}] {record.eventName}");
            GUILayout.Label($"From: {record.sender}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("清空历史")) manager.history.Clear();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        
        // 强制刷新界面以实现实时更新
        Repaint(); 
    }

    // --- 文件操作逻辑 ---

    private void LoadEventFile()
    {
        existingEvents.Clear();
        if (!File.Exists(EVENT_FILE_PATH)) return;

        string content = File.ReadAllText(EVENT_FILE_PATH);
        // 正则匹配 public const string XXX = "XXX";
        var matches = Regex.Matches(content, @"public\s+const\s+string\s+(\w+)\s*=");
        
        foreach (Match match in matches)
        {
            existingEvents.Add(match.Groups[1].Value);
        }
    }

    private void AddEventToFile(string evtName)
    {
        if (string.IsNullOrEmpty(evtName)) return;
        if (existingEvents.Contains(evtName)) { ShowNotification(new GUIContent("事件名已存在")); return; }
        if (!File.Exists(EVENT_FILE_PATH)) { ShowNotification(new GUIContent("找不到文件")); return; }

        // 读取所有行
        var lines = File.ReadAllLines(EVENT_FILE_PATH).ToList();
        
        // 找到最后一行并插入 (假设最后一行是 })
        int insertIndex = lines.Count - 1;
        // 简单处理：插入到最后一个 } 之前
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (lines[i].Trim() == "}")
            {
                insertIndex = i;
                break;
            }
        }

        string newCode = $"    public const string {evtName} = \"{evtName}\";";
        lines.Insert(insertIndex, newCode);

        File.WriteAllLines(EVENT_FILE_PATH, lines);
        AssetDatabase.Refresh();
        
        newEventName = "";
        LoadEventFile();
        ShowNotification(new GUIContent("添加成功"));
    }
}
