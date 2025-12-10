#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// --- 1. 快速场景切换 (菜单栏实现) ---
public static class QuickSceneSwitcher
{
    // 动态生成菜单项
    [MenuItem("Tools/Scenes/Load Scenes...", false, 0)]
    public static void ShowSceneMenu() { }

    // 验证函数：在这里动态添加子菜单
    [MenuItem("Tools/Scenes/Load Scenes...", true)]
    public static bool ShowSceneMenuValidate()
    {
        Menu.SetChecked("Tools/Scenes/Load Scenes...", false);
        return true;
    }

    // 注意：Unity原生不支持动态生成顶级菜单，这里用一个简单变通
    // 推荐把当前 Build Settings 里的场景列出来
    [MenuItem("Tools/Scenes/Open Build Settings Scenes", false, 1)]
    public static void OpenBuildSettingsScene()
    {
        var menu = new GenericMenu();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                string path = scene.path;
                string name = Path.GetFileNameWithoutExtension(path);
                menu.AddItem(new GUIContent(name), false, () => {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(path);
                    }
                });
            }
        }
        menu.ShowAsContext();
    }
}

// --- 2. 代码行数统计 ---
public static class CodeLineCounter
{
    [MenuItem("Tools/统计代码行数 (C#)", false, 50)]
    public static void CountLines()
    {
        string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        int totalLines = 0;
        int fileCount = 0;

        foreach (var file in files)
        {
            // 排除临时文件或特定文件夹
            if (file.Contains("Plugins") || file.Contains("Lib")) continue;

            var lines = File.ReadAllLines(file);
            // 简单的过滤空行和注释
            totalLines += lines.Count(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//"));
            fileCount++;
        }

        EditorUtility.DisplayDialog("代码统计", 
            $"项目中 (Assets下) 共有 C# 文件: {fileCount} 个\n有效代码行数: {totalLines} 行", "牛逼");
    }
}

// --- 3. 待办事项便签 (To-Do) ---
public class ToDoListWindow : EditorWindow
{
    private string noteText = "";
    private Vector2 scrollPos;

    [MenuItem("Tools/待办事项便签 (To-Do)", false, 51)]
    public static void ShowWindow()
    {
        GetWindow<ToDoListWindow>("待办清单").Show();
    }

    private void OnEnable()
    {
        noteText = EditorPrefs.GetString("SimpleToDoList_Data", "- 修复 Bug A\n- 调整 UI 布局");
    }

    private void OnDisable()
    {
        EditorPrefs.SetString("SimpleToDoList_Data", noteText);
    }

    private void OnGUI()
    {
        GUILayout.Label("📝 开发备忘录", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        noteText = EditorGUILayout.TextArea(noteText, GUILayout.Height(position.height - 40));
        EditorGUILayout.EndScrollView();
    }
}

// --- 4. 资源收藏夹 (Favorites) ---
public class FavoritesWindow : EditorWindow
{
    [System.Serializable]
    public class FavItem { public string guid; }
    
    private List<string> favorites = new List<string>();

    [MenuItem("Tools/资源收藏夹 (Favorites)", false, 52)]
    public static void ShowWindow()
    {
        GetWindow<FavoritesWindow>("收藏夹").Show();
    }

    private void OnEnable()
    {
        string data = EditorPrefs.GetString("SimpleFavorites_Data", "");
        if (!string.IsNullOrEmpty(data)) favorites = new List<string>(data.Split(';'));
    }

    private void OnDisable()
    {
        EditorPrefs.SetString("SimpleFavorites_Data", string.Join(";", favorites));
    }

    private void OnGUI()
    {
        // 拖拽区域
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "拖拽资源到此处添加收藏", EditorStyles.helpBox);
        HandleDragDrop(dropArea);

        GUILayout.Space(10);

        for (int i = 0; i < favorites.Count; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(favorites[i]);
            if (string.IsNullOrEmpty(path)) 
            {
                favorites.RemoveAt(i--); // 清理无效引用
                continue;
            }

            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
            
            EditorGUILayout.ObjectField(obj, typeof(Object), false);
            
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                favorites.RemoveAt(i);
                return;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void HandleDragDrop(Rect dropArea)
    {
        Event current = Event.current;
        if (!dropArea.Contains(current.mousePosition)) return;

        if (current.type == EventType.DragUpdated || current.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (!favorites.Contains(guid)) favorites.Add(guid);
                }
            }
            Event.current.Use();
        }
    }
}
#endif