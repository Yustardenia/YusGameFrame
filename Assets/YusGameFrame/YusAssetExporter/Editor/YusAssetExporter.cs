using UnityEngine;
using UnityEditor;
using System.IO;

public static class YusAssetExporter
{
    // 配置：是否导出 .meta 文件 (通常导出给非Unity人员不需要meta文件)
    private static bool ExportMetaFiles = false;

    [MenuItem("Assets/Yus Tools/📂 导出选中内容到指定文件夹", false, 20)]
    public static void ExportSelectedAssets()
    {
        // 1. 获取选中的对象
        string[] guids = Selection.assetGUIDs;
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口选中要导出的文件或文件夹。", "确定");
            return;
        }

        // 2. 选择保存路径
        string exportRootPath = EditorUtility.OpenFolderPanel("选择导出目标文件夹", "", "");
        if (string.IsNullOrEmpty(exportRootPath)) return; // 用户取消

        int totalCount = guids.Length;
        int currentCount = 0;

        try
        {
            foreach (string guid in guids)
            {
                // 获取 Unity 相对路径 (Assets/...)
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // 获取系统绝对路径
                string sourceFullPath = Path.GetFullPath(assetPath);
                
                // 获取文件名或文件夹名
                string fileName = Path.GetFileName(assetPath);

                // 更新进度条
                currentCount++;
                EditorUtility.DisplayProgressBar("正在导出...", $"正在处理: {fileName} ({currentCount}/{totalCount})", (float)currentCount / totalCount);

                // 判断是文件还是文件夹
                FileAttributes attr = File.GetAttributes(sourceFullPath);
                
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    // --- 是文件夹：递归复制 ---
                    string destFolderPath = Path.Combine(exportRootPath, fileName);
                    CopyDirectory(sourceFullPath, destFolderPath);
                }
                else
                {
                    // --- 是文件：直接复制 ---
                    string destFilePath = Path.Combine(exportRootPath, fileName);
                    File.Copy(sourceFullPath, destFilePath, true); // true = 覆盖已存在文件
                    
                    // 如果需要导出 meta 文件
                    if (ExportMetaFiles)
                    {
                        string metaSource = sourceFullPath + ".meta";
                        string metaDest = destFilePath + ".meta";
                        if (File.Exists(metaSource)) File.Copy(metaSource, metaDest, true);
                    }
                }
            }

            Debug.Log($"<color=green>[YusExporter] 导出完成！已保存到: {exportRootPath}</color>");
            
            // 导出完成后自动打开文件夹
            EditorUtility.RevealInFinder(exportRootPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[YusExporter] 导出失败: {ex.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// 递归复制文件夹
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        // 创建目标文件夹
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // 获取源文件夹中的所有文件
        string[] files = Directory.GetFiles(sourceDir);
        foreach (string file in files)
        {
            if (!ExportMetaFiles && file.EndsWith(".meta")) continue; // 跳过 meta 文件

            string name = Path.GetFileName(file);
            string dest = Path.Combine(destDir, name);
            File.Copy(file, dest, true); // true = 覆盖
        }

        // 获取源文件夹中的所有子文件夹
        string[] dirs = Directory.GetDirectories(sourceDir);
        foreach (string dir in dirs)
        {
            string name = Path.GetFileName(dir);
            string dest = Path.Combine(destDir, name);
            CopyDirectory(dir, dest); // 递归调用
        }
    }
}