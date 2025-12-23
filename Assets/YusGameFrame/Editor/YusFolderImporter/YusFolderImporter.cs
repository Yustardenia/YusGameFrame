using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class YusFolderImporter : EditorWindow
{
    private string sourcePath = "";
    private string targetPath = "Assets"; // 默认导入到 Assets 根目录
    private bool copyMeta = true; // 极其重要：是否导入 .meta 文件

    [MenuItem(YusGameFrameEditorMenu.Root + "Tools/文件夹导入向导 (Folder Importer)", false, 50)]
    public static void ShowWindow()
    {
        GetWindow<YusFolderImporter>("导入向导");
    }

    private void OnGUI()
    {
        GUILayout.Label("📥 外部文件夹导入工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("替代 .unitypackage！\n直接将外部文件夹的内容复制到项目中。\n支持增量修改，就像操作普通文件一样。", MessageType.Info);

        GUILayout.Space(10);

        // --- 1. 源文件夹选择 ---
        EditorGUILayout.LabelField("1. 选择外部源文件夹:");
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label(string.IsNullOrEmpty(sourcePath) ? "未选择..." : sourcePath, EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("选择...", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择要导入的文件夹", "", "");
            if (!string.IsNullOrEmpty(path)) sourcePath = path;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // --- 2. 目标路径选择 ---
        EditorGUILayout.LabelField("2. 导入到 Unity 的哪个位置:");
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label(targetPath, EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("设为当前选中", GUILayout.Width(100)))
        {
            SetTargetToSelection();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // --- 3. 选项 ---
        copyMeta = EditorGUILayout.ToggleLeft("同时导入 .meta 文件 (推荐勾选，防断引用)", copyMeta);

        GUILayout.FlexibleSpace();

        // --- 4. 执行 ---
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("🚀 开始导入", GUILayout.Height(40)))
        {
            ImportFolder();
        }
        GUI.backgroundColor = Color.white;
    }

    private void SetTargetToSelection()
    {
        if (Selection.activeObject != null)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (AssetDatabase.IsValidFolder(path))
            {
                targetPath = path;
                return;
            }
        }
        targetPath = "Assets";
        ShowNotification(new GUIContent("请在 Project 窗口选中一个文件夹"));
    }

    private void ImportFolder()
    {
        if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
        {
            EditorUtility.DisplayDialog("错误", "源文件夹无效或不存在。", "OK");
            return;
        }

        string folderName = Path.GetFileName(sourcePath);
        string finalDestDir = Path.Combine(targetPath, folderName);

        // 确认提示
        if (AssetDatabase.IsValidFolder(finalDestDir))
        {
            if (!EditorUtility.DisplayDialog("覆盖确认", 
                $"目标文件夹 '{folderName}' 已存在于 '{targetPath}'。\n\n是否覆盖/合并？", "继续", "取消"))
            {
                return;
            }
        }

        try
        {
            int count = 0;
            CopyDirectory(sourcePath, finalDestDir, ref count);
            
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>[YusImporter] 导入成功！共处理 {count} 个文件。</color>");
            
            // 导入后高亮该文件夹
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(finalDestDir);
            EditorGUIUtility.PingObject(obj);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导入失败: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void CopyDirectory(string sourceDir, string destDir, ref int fileCount)
    {
        if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

        // 1. 复制文件
        string[] files = Directory.GetFiles(sourceDir);
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            
            // 忽略系统生成的隐藏文件 (Mac 的 .DS_Store 等)
            if (fileName.StartsWith(".")) continue;
            // 如果不导 meta，且当前是 meta，跳过
            if (!copyMeta && fileName.EndsWith(".meta")) continue;

            string destFile = Path.Combine(destDir, fileName);
            
            // 更新进度条
            fileCount++;
            if (fileCount % 10 == 0) // 每10个文件刷新一次进度条，提高性能
            {
                EditorUtility.DisplayProgressBar("导入中", $"正在复制: {fileName}", 0.5f);
            }

            File.Copy(file, destFile, true); // true = 覆盖
        }

        // 2. 递归复制子文件夹
        string[] dirs = Directory.GetDirectories(sourceDir);
        foreach (string dir in dirs)
        {
            string dirName = Path.GetFileName(dir);
            string destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir, ref fileCount);
        }
    }
}
