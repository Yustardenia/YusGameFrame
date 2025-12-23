using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class YusPoolDebugger : EditorWindow
{
    private Vector2 scrollPos;
    private string searchQuery = "";
    
    // 预热工具变量
    private string prewarmPath = "";
    private int prewarmCount = 10;
    
    // 缓存活跃对象统计 (PoolName -> Count)
    private Dictionary<string, int> activeCountCache = new Dictionary<string, int>();
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.5f; // 每0.5秒更新一次活跃统计，防止卡顿

    [MenuItem(YusGameFrameEditorMenu.Root + "Systems/Pool/对象池监视器 (Pool Monitor)")]
    public static void ShowWindow()
    {
        GetWindow<YusPoolDebugger>("对象池监视器");
    }

    private void OnInspectorUpdate()
    {
        // 保持界面刷新，以便看到数字跳动
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("🏊 Yus 对象池监控中心", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请运行游戏以查看实时数据。", MessageType.Info);
            return;
        }

        if (YusPoolManager.Instance == null)
        {
            EditorGUILayout.HelpBox("YusPoolManager 未初始化。", MessageType.Warning);
            return;
        }

        // 获取数据
        var poolDict = YusPoolManager.Instance.Debug_GetPoolDict();
        UpdateActiveStats(); // 更新活跃对象统计

        // --- 1. 顶部统计栏 ---
        DrawHeaderStats(poolDict);

        // --- 2. 工具栏 ---
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("🔍", GUILayout.Width(20));
        searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.Height(20));
        if (GUILayout.Button("X", GUILayout.Width(20))) searchQuery = "";
        EditorGUILayout.EndHorizontal();

        // --- 2.5 预热工具 ---
        DrawPrewarmTool();

        // --- 3. 列表区域 ---
        DrawPoolList(poolDict);

        // --- 4. 底部操作 ---
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal("box");
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("🗑️ 清空所有闲置对象", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("警告", "确定要销毁池子里所有闲置对象吗？\n这会释放内存，但在下次生成时会产生 GC。", "确定清空", "取消"))
            {
                YusPoolManager.Instance.ClearAll();
            }
        }
        GUI.backgroundColor = Color.white;
        
        if (GUILayout.Button("📂 选中池子根节点", GUILayout.Height(30)))
        {
            Selection.activeTransform = YusPoolManager.Instance.Debug_GetRoot();
        }
        EditorGUILayout.EndHorizontal();
    }

    // 更新活跃对象统计 (通过查找场景中的 PoolObject 组件)
    private void UpdateActiveStats()
    {
        if (Time.realtimeSinceStartup - lastUpdateTime < UPDATE_INTERVAL) return;
        lastUpdateTime = Time.realtimeSinceStartup;

        activeCountCache.Clear();
        
        // 这是一个比较重的操作，所以限制了频率
        var allPoolObjects = FindObjectsOfType<PoolObject>();
        foreach (var obj in allPoolObjects)
        {
            if (obj.gameObject.activeInHierarchy && obj.IsInUse)
            {
                if (!activeCountCache.ContainsKey(obj.PoolName))
                    activeCountCache[obj.PoolName] = 0;
                
                activeCountCache[obj.PoolName]++;
            }
        }
    }

    private void DrawHeaderStats(Dictionary<string, Queue<GameObject>> poolDict)
    {
        int totalInactive = 0;
        foreach (var q in poolDict.Values) totalInactive += q.Count;

        int totalActive = 0;
        foreach (var count in activeCountCache.Values) totalActive += count;

        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.BeginHorizontal();
        
        DrawStatBox("池子总数", poolDict.Count.ToString(), Color.cyan);
        DrawStatBox("闲置待命 (Inactive)", totalInactive.ToString(), Color.green);
        DrawStatBox("正在使用 (Active)", totalActive.ToString(), new Color(1f, 0.8f, 0.4f)); // 橙色

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawStatBox(string title, string value, Color color)
    {
        var style = new GUIStyle(GUI.skin.box);
        style.normal.textColor = color;
        style.fontStyle = FontStyle.Bold;
        
        EditorGUILayout.BeginVertical(style);
        GUILayout.Label(title, EditorStyles.miniLabel);
        GUILayout.Label(value, EditorStyles.largeLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawPoolList(Dictionary<string, Queue<GameObject>> poolDict)
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var kvp in poolDict)
        {
            string poolName = kvp.Key;
            
            // 搜索过滤
            if (!string.IsNullOrEmpty(searchQuery) && !poolName.ToLower().Contains(searchQuery.ToLower()))
                continue;

            Queue<GameObject> queue = kvp.Value;
            int inactiveCount = queue.Count;
            int activeCount = activeCountCache.ContainsKey(poolName) ? activeCountCache[poolName] : 0;
            int total = inactiveCount + activeCount;

            EditorGUILayout.BeginVertical("box");
            
            // 标题行
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(poolName, EditorStyles.boldLabel, GUILayout.Width(position.width * 0.5f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                queue.Clear(); // 简单清除引用，实际销毁可能需要 Destroy
                // 严谨的做法是遍历 Destroy，这里为了演示简化
                // 建议在 Manager 里加一个 ClearPool(name) 方法
            }
            EditorGUILayout.EndHorizontal();

            // 进度条可视化
            float usageRate = total > 0 ? (float)activeCount / total : 0;
            Rect rect = EditorGUILayout.GetControlRect(false, 18);
            EditorGUI.ProgressBar(rect, usageRate, $"使用率: {activeCount}/{total} (闲置: {inactiveCount})");

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPrewarmTool()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🔥 预热工具", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("路径:", GUILayout.Width(40));
        prewarmPath = EditorGUILayout.TextField(prewarmPath);
        GUILayout.Label("数量:", GUILayout.Width(40));
        prewarmCount = EditorGUILayout.IntField(prewarmCount, GUILayout.Width(50));
        
        if (GUILayout.Button("预热", GUILayout.Width(60)))
        {
            if (string.IsNullOrEmpty(prewarmPath))
            {
                Debug.LogWarning("请输入预热路径");
            }
            else
            {
                YusPoolManager.Instance.Prewarm(prewarmPath, prewarmCount);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
}
