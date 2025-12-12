using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class YusSaveEditorWindow : EditorWindow
{
    [MenuItem("Tools/Yus Data/11. 存档管理 (Save Manager)")]
    public static void ShowWindow()
    {
        GetWindow<YusSaveEditorWindow>("存档管理");
    }

    private string savePath;
    private List<SaveFileInfo> saveFiles = new List<SaveFileInfo>();
    private Vector2 scrollPos;

    private struct SaveFileInfo
    {
        public string FileName;
        public string FullPath;
        public int Version;
        public int ItemCount;
        public System.DateTime LastWriteTime;
        public long Size;
        public bool IsValid;
    }

    private void OnEnable()
    {
        // 获取 YusDataManager 中的路径 (需要反射或者直接硬编码，这里为了稳健直接用同样的逻辑)
        savePath = Application.persistentDataPath + "/SaveData/";
        RefreshList();
    }

    private void OnGUI()
    {
        GUILayout.Label("二进制存档管理", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"路径: {savePath}", EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("打开文件夹", GUILayout.Width(80)))
        {
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            EditorUtility.RevealInFinder(savePath);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("刷新列表"))
        {
            RefreshList();
        }

        GUILayout.Space(10);

        if (saveFiles.Count == 0)
        {
            GUILayout.Label("暂无存档文件");
            return;
        }

        // 表头
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("文件名", GUILayout.Width(150));
        GUILayout.Label("版本", GUILayout.Width(40));
        GUILayout.Label("数量", GUILayout.Width(40));
        GUILayout.Label("大小", GUILayout.Width(60));
        GUILayout.Label("修改时间", GUILayout.Width(120));
        GUILayout.Label("操作", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var file in saveFiles)
        {
            EditorGUILayout.BeginHorizontal("box");
            
            // 文件名
            GUILayout.Label(file.FileName, GUILayout.Width(150));

            if (file.IsValid)
            {
                GUILayout.Label(file.Version.ToString(), GUILayout.Width(40));
                GUILayout.Label(file.ItemCount.ToString(), GUILayout.Width(40));
            }
            else
            {
                GUILayout.Label("Err", GUILayout.Width(40));
                GUILayout.Label("-", GUILayout.Width(40));
            }

            // 大小
            GUILayout.Label(EditorUtility.FormatBytes(file.Size), GUILayout.Width(60));
            
            // 时间
            GUILayout.Label(file.LastWriteTime.ToString("MM-dd HH:mm"), GUILayout.Width(120));

            // 删除按钮
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("删除", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("删除存档", $"确定要删除 {file.FileName} 吗？此操作不可恢复。", "删除", "取消"))
                {
                    File.Delete(file.FullPath);
                    RefreshList();
                    GUIUtility.ExitGUI(); // 防止报错
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void RefreshList()
    {
        saveFiles.Clear();
        if (!Directory.Exists(savePath)) return;

        string[] files = Directory.GetFiles(savePath, "*.yus");
        foreach (var filePath in files)
        {
            SaveFileInfo info = new SaveFileInfo();
            info.FullPath = filePath;
            info.FileName = Path.GetFileName(filePath);
            
            FileInfo fi = new FileInfo(filePath);
            info.LastWriteTime = fi.LastWriteTime;
            info.Size = fi.Length;
            info.IsValid = false;

            // 尝试读取头部信息
            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    string magic = br.ReadString();
                    if (magic == "YUS_SAVE")
                    {
                        info.Version = br.ReadInt32();
                        info.ItemCount = br.ReadInt32();
                        info.IsValid = true;
                    }
                }
            }
            catch
            {
                // 读取失败算无效文件
            }

            saveFiles.Add(info);
        }
    }
}
