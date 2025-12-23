using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class YusAdvancedExporter : EditorWindow
{
    // 数据类：存储待导出文件的信息
    private class ExportItem
    {
        public string assetPath;    // Assets/Scripts/Player.cs
        public bool isSelected;     // 是否勾选
        public string fileName;     // Player.cs
        public Texture icon;        // 文件图标
    }

    private List<ExportItem> items = new List<ExportItem>();
    private Vector2 scrollPos;
    private bool exportMeta = false; // 是否导出 meta 文件

    [MenuItem(YusGameFrameEditorMenu.Root + "Assets/高级导出向导 (Advanced Exporter)", false, 21)]
    public static void ShowWindow()
    {
        YusAdvancedExporter window = GetWindow<YusAdvancedExporter>("高级导出");
        window.minSize = new Vector2(400, 500);
        window.ScanSelection(); // 打开时自动扫描当前选中的内容
    }

    private void OnGUI()
    {
        GUILayout.Label("📂 资源导出向导", EditorStyles.boldLabel);

        // --- 1. 工具栏区域 ---
        EditorGUILayout.BeginHorizontal("box");
        if (GUILayout.Button("🔄 重新扫描选中项", GUILayout.Height(30)))
        {
            ScanSelection();
        }
        
        // 快捷功能区
        if (GUILayout.Button("只选脚本 (.cs)", GUILayout.Height(30)))
        {
            ApplyFilter(".cs");
        }
        EditorGUILayout.EndHorizontal();

        // 辅助选择
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选")) SetAll(true);
        if (GUILayout.Button("全不选")) SetAll(false);
        if (GUILayout.Button("反选")) InvertSelection();
        EditorGUILayout.EndHorizontal();

        // --- 2. 列表区域 ---
        GUILayout.Space(10);
        GUILayout.Label($"待导出列表 ({items.Count(x => x.isSelected)} / {items.Count})", EditorStyles.boldLabel);
        
        // 绘制表头
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("√", GUILayout.Width(20));
        GUILayout.Label("文件名称", GUILayout.ExpandWidth(true));
        GUILayout.Label("类型", GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        if (items.Count == 0)
        {
            GUILayout.Label("请在 Project 窗口选中文件或文件夹，然后点击“重新扫描”");
        }

        for (int i = 0; i < items.Count; i++)
        {
            DrawItem(items[i]);
        }

        EditorGUILayout.EndScrollView();

        // --- 3. 导出设置区域 ---
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical("box");
        exportMeta = EditorGUILayout.Toggle("同时导出 .meta 文件", exportMeta);
        
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("🚀 导出选中文件到...", GUILayout.Height(40)))
        {
            StartExport();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
    }

    // --- 逻辑方法 ---

    // 绘制单行列表项
    private void DrawItem(ExportItem item)
    {
        EditorGUILayout.BeginHorizontal();
        
        // 复选框
        item.isSelected = EditorGUILayout.Toggle(item.isSelected, GUILayout.Width(20));

        // 图标 + 名字
        GUIContent content = new GUIContent(item.fileName, item.icon);
        GUILayout.Label(content, GUILayout.Height(20), GUILayout.ExpandWidth(true));

        // 后缀名提示
        string ext = Path.GetExtension(item.fileName);
        GUILayout.Label(ext, EditorStyles.miniLabel, GUILayout.Width(50));

        EditorGUILayout.EndHorizontal();
    }

    // 扫描当前选中的资源
    private void ScanSelection()
    {
        items.Clear();
        string[] guids = Selection.assetGUIDs;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // 判断是否是文件夹
            if (AssetDatabase.IsValidFolder(path))
            {
                // 递归获取文件夹下所有文件
                string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    // 统一路径分隔符，并过滤掉 .meta (除非后面导出逻辑需要，但在列表中我们通常不显示meta)
                    string unityPath = file.Replace("\\", "/");
                    if (unityPath.EndsWith(".meta")) continue;
                    
                    AddItem(unityPath);
                }
            }
            else
            {
                // 是文件，直接添加
                AddItem(path);
            }
        }
    }

    private void AddItem(string path)
    {
        // 避免重复添加
        if (items.Any(x => x.assetPath == path)) return;

        items.Add(new ExportItem
        {
            assetPath = path,
            fileName = Path.GetFileName(path),
            isSelected = true, // 默认全选
            icon = AssetDatabase.GetCachedIcon(path)
        });
    }

    // 快捷过滤器
    private void ApplyFilter(string extension)
    {
        foreach (var item in items)
        {
            item.isSelected = item.fileName.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SetAll(bool select)
    {
        foreach (var item in items) item.isSelected = select;
    }

    private void InvertSelection()
    {
        foreach (var item in items) item.isSelected = !item.isSelected;
    }

    // 执行导出
    private void StartExport()
    {
        var selectedItems = items.Where(x => x.isSelected).ToList();
        if (selectedItems.Count == 0)
        {
            ShowNotification(new GUIContent("未选中任何文件"));
            return;
        }

        string exportRoot = EditorUtility.OpenFolderPanel("选择保存位置", "", "");
        if (string.IsNullOrEmpty(exportRoot)) return;

        int count = 0;
        try
        {
            foreach (var item in selectedItems)
            {
                count++;
                EditorUtility.DisplayProgressBar("导出中", $"正在复制: {item.fileName}", (float)count / selectedItems.Count);

                string sourcePath = Path.GetFullPath(item.assetPath);
                
                // 保持 Assets 下的目录结构
                // 例如 item.assetPath = "Assets/Scripts/Manager/Game.cs"
                // 我们希望导出到 = "目标文件夹/Scripts/Manager/Game.cs"
                // 所以要把开头的 "Assets/" 去掉
                string relativePath = item.assetPath;
                if (relativePath.StartsWith("Assets/")) relativePath = relativePath.Substring(7);

                string destPath = Path.Combine(exportRoot, relativePath);
                
                // 确保目标文件夹存在
                string destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                // 复制主文件
                File.Copy(sourcePath, destPath, true);

                // 复制 Meta 文件 (如果勾选)
                if (exportMeta)
                {
                    if (File.Exists(sourcePath + ".meta"))
                    {
                        File.Copy(sourcePath + ".meta", destPath + ".meta", true);
                    }
                }
            }
            
            EditorUtility.RevealInFinder(exportRoot);
            ShowNotification(new GUIContent("导出成功！"));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导出出错: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
