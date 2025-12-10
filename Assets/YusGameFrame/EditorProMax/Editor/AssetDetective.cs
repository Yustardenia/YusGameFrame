#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

public class AssetDetective : EditorWindow
{
    private enum Mode { References, Unused, Duplicates }
    private Mode currentMode;
    private List<string> results = new List<string>();
    private string selectedAssetGuid;
    private Vector2 scrollPos;

    // --- 1. 查找引用 (Reference Finder) ---
    [MenuItem("Assets/Asset Detective/🔍 查找谁引用了我 (Find References)", false, 20)]
    private static void FindReferences()
    {
        var window = GetWindow<AssetDetective>("资源侦探");
        window.currentMode = Mode.References;
        window.selectedAssetGuid = Selection.assetGUIDs[0];
        window.ScanReferences();
        window.Show();
    }

    // --- 2. 查找废弃资源 (Unused Asset Finder) ---
    [MenuItem("Tools/Asset Detective/🗑️ 查找废弃资源 (Unused Assets)", false, 0)]
    private static void FindUnused()
    {
        var window = GetWindow<AssetDetective>("资源侦探");
        window.currentMode = Mode.Unused;
        window.ScanUnused();
        window.Show();
    }

    // --- 3. 查找重复资源 (Duplicate Finder) ---
    [MenuItem("Tools/Asset Detective/👯 查找重复资源 (Duplicate Finder)", false, 1)]
    private static void FindDuplicates()
    {
        var window = GetWindow<AssetDetective>("资源侦探");
        window.currentMode = Mode.Duplicates;
        window.ScanDuplicates();
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label(currentMode.ToString(), EditorStyles.largeLabel);

        if (GUILayout.Button("Clear Results")) results.Clear();
        if (currentMode == Mode.Unused) GUILayout.Label("注意：无法检测 Resources.Load 动态加载的资源！谨慎删除！", EditorStyles.helpBox);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var path in results)
        {
            EditorGUILayout.BeginHorizontal();
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            EditorGUILayout.ObjectField(obj, typeof(Object), false);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    // --- 逻辑实现区域 ---

    private void ScanReferences()
    {
        results.Clear();
        string targetPath = AssetDatabase.GUIDToAssetPath(selectedAssetGuid);
        if (string.IsNullOrEmpty(targetPath)) return;

        string[] allGuids = AssetDatabase.FindAssets(""); // 扫描所有资源
        EditorUtility.DisplayProgressBar("扫描引用", "正在遍历工程...", 0);

        int count = 0;
        foreach (var guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == targetPath) continue;
            
            // 这是一个比较耗时的操作，大项目慎用，或者优化为只扫描Prefab/Scene
            string[] dependencies = AssetDatabase.GetDependencies(path, false);
            foreach (var dep in dependencies)
            {
                if (dep == targetPath)
                {
                    results.Add(path);
                    break;
                }
            }
            count++;
            if(count % 100 == 0) EditorUtility.DisplayProgressBar("扫描引用", $"分析中: {path}", (float)count / allGuids.Length);
        }
        EditorUtility.ClearProgressBar();
    }

    private void ScanUnused()
    {
        results.Clear();
        // 1. 获取 Build Settings 里所有的 Scene
        var usedAssets = new HashSet<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                usedAssets.Add(scene.path);
                foreach (var dep in AssetDatabase.GetDependencies(scene.path)) usedAssets.Add(dep);
            }
        }

        // 2. 获取 Resources 文件夹下的所有资源 (因为它们会被无条件打包)
        var resourceGuids = AssetDatabase.FindAssets("", new[] { "Assets" }); 
        foreach (var guid in resourceGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Resources/"))
            {
                usedAssets.Add(path);
                foreach (var dep in AssetDatabase.GetDependencies(path)) usedAssets.Add(dep);
            }
        }

        // 3. 对比所有资源
        var allAssetGuids = AssetDatabase.FindAssets("t:Prefab t:Texture t:Material t:AudioClip t:Model"); // 只扫描特定类型，忽略脚本
        foreach (var guid in allAssetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!usedAssets.Contains(path) && !path.Contains("/Editor/")) // 忽略 Editor 资源
            {
                results.Add(path);
            }
        }
    }

    private void ScanDuplicates()
    {
        results.Clear();
        var allAssetGuids = AssetDatabase.FindAssets("t:Texture t:AudioClip"); // 主要检查贴图和音频
        var hashDict = new Dictionary<string, string>(); // Hash -> Path

        EditorUtility.DisplayProgressBar("查重", "计算 Hash 值...", 0);
        int i = 0;
        using (var md5 = MD5.Create())
        {
            foreach (var guid in allAssetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if(File.Exists(path))
                {
                    byte[] data = File.ReadAllBytes(path);
                    byte[] hashBytes = md5.ComputeHash(data);
                    string hash = System.BitConverter.ToString(hashBytes);

                    if (hashDict.ContainsKey(hash))
                    {
                        results.Add($"[重复] {path}  <==>  {hashDict[hash]}");
                    }
                    else
                    {
                        hashDict[hash] = path;
                    }
                }
                i++;
                if (i % 50 == 0) EditorUtility.DisplayProgressBar("查重", path, (float)i / allAssetGuids.Length);
            }
        }
        EditorUtility.ClearProgressBar();
    }
}
#endif