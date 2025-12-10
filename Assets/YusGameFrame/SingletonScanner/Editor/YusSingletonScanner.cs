using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class YusSingletonScanner : EditorWindow
{
    // 数据类：存储扫描结果
    private class SingletonInfo
    {
        public string scriptName;
        public MonoScript scriptAsset;
        public Type type;
        public GameObject sceneInstance; // 场景里的实例
        public bool isMissing;           // 是否缺失
    }

    private DefaultAsset searchFolder; // 搜索目录
    private List<SingletonInfo> results = new List<SingletonInfo>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Yus Data/7. 单例检查器 (Singleton Scanner)")]
    public static void ShowWindow()
    {
        GetWindow<YusSingletonScanner>("单例检查器");
    }

    private void OnEnable()
    {
        // 默认搜索 Scripts 文件夹，如果没有则搜索 Assets
        string defaultPath = "Assets/Scripts";
        if (AssetDatabase.IsValidFolder(defaultPath))
        {
            searchFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultPath);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("🕵️‍♂️ 单例模式脚本扫描", EditorStyles.boldLabel);

        // --- 1. 设置区域 ---
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("扫描文件夹:", GUILayout.Width(80));
        searchFolder = (DefaultAsset)EditorGUILayout.ObjectField(searchFolder, typeof(DefaultAsset), false);
        
        if (GUILayout.Button("开始扫描", GUILayout.Height(20), GUILayout.Width(80)))
        {
            ScanProject();
        }
        EditorGUILayout.EndHorizontal();

        // --- 2. 统计信息 ---
        if (results.Count > 0)
        {
            int missingCount = results.Count(x => x.isMissing);
            string status = missingCount > 0 
                ? $"<color=red>发现 {missingCount} 个单例未挂载！</color>" 
                : "<color=green>所有单例均已挂载。</color>";
            
            EditorGUILayout.LabelField($"扫描到 {results.Count} 个单例脚本。{status}", new GUIStyle(EditorStyles.label) { richText = true });
        }

        // --- 3. 列表区域 ---
        GUILayout.Space(10);
        DrawHeader();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        foreach (var info in results)
        {
            DrawItem(info);
        }

        if (results.Count == 0 && searchFolder != null)
        {
            GUILayout.Label("暂无数据，请点击扫描。", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("脚本名称", EditorStyles.boldLabel, GUILayout.Width(200));
        GUILayout.Label("状态 / 场景实例", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
        GUILayout.Label("操作", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawItem(SingletonInfo info)
    {
        // 根据状态决定颜色
        GUI.color = info.isMissing ? new Color(1f, 0.6f, 0.6f) : Color.white; // 缺失变红
        
        EditorGUILayout.BeginHorizontal("helpbox");
        GUI.color = Color.white; // 恢复颜色绘制内容

        // 1. 脚本图标和名字
        EditorGUIUtility.SetIconSize(new Vector2(16, 16));
        var icon = EditorGUIUtility.ObjectContent(null, typeof(MonoScript)).image;
        GUILayout.Label(new GUIContent(info.scriptName, icon), GUILayout.Width(200));

        // 2. 状态显示
        if (info.isMissing)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = Color.red;
            GUILayout.Label("❌ 场景中未找到实例", style, GUILayout.ExpandWidth(true));
        }
        else
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = new Color(0, 0.5f, 0); // 深绿
            if (GUILayout.Button($"✅ {info.sceneInstance.name}", style, GUILayout.ExpandWidth(true)))
            {
                EditorGUIUtility.PingObject(info.sceneInstance);
                Selection.activeGameObject = info.sceneInstance;
            }
        }

        // 3. 操作按钮
        if (GUILayout.Button("脚本", GUILayout.Width(60)))
        {
            EditorGUIUtility.PingObject(info.scriptAsset);
        }

        EditorGUILayout.EndHorizontal();
    }

    // --- 核心扫描逻辑 ---
    private void ScanProject()
    {
        results.Clear();
        
        string path = "Assets";
        if (searchFolder != null) path = AssetDatabase.GetAssetPath(searchFolder);

        // 1. 获取目录下所有 .cs 文件
        string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { path });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            
            if (script == null) continue;

            // 获取脚本对应的 C# 类
            Type type = script.GetClass();
            if (type == null) continue;

            // 2. 判定逻辑：
            // A. 必须继承 MonoBehaviour
            // B. 不是抽象类
            // C. 包含名为 "Instance" 的静态属性或字段
            if (IsMonoSingleton(type))
            {
                // 3. 检查场景是否存在
                // FindObjectOfType 性能较低，但这是编辑器操作，可以接受
                // 对于泛型单例 YusBaseManager<T,K>，FindObjectOfType 能正确找到具体的子类
                UnityEngine.Object sceneObj = FindObjectOfType(type);

                SingletonInfo info = new SingletonInfo
                {
                    scriptName = type.Name,
                    scriptAsset = script,
                    type = type,
                    sceneInstance = sceneObj as GameObject,
                    isMissing = (sceneObj == null)
                };
                
                // 如果找到了组件，获取它挂载的 GameObject
                if (sceneObj != null)
                {
                    info.sceneInstance = (sceneObj as Component).gameObject;
                }

                results.Add(info);
            }
        }
    }

    private bool IsMonoSingleton(Type type)
    {
        // 必须继承 MonoBehaviour
        if (!type.IsSubclassOf(typeof(MonoBehaviour))) return false;
        
        // 排除抽象类 (比如 YusBaseManager 本身)
        if (type.IsAbstract) return false;

        // 查找名为 "Instance" 的公共静态属性或字段
        // BindingFlags.FlattenHierarchy 很重要，它能让我们查到父类(YusBaseManager)里的 Instance
        var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        
        var prop = type.GetProperty("Instance", flags);
        if (prop != null) return true;

        var field = type.GetField("Instance", flags);
        if (field != null) return true;

        // 兼容有些人习惯用 _instance
        var fieldPrivate = type.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (fieldPrivate != null) return true;

        return false;
    }
}