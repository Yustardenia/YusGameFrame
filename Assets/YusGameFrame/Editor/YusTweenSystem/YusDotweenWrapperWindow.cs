#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class YusDotweenWrapperWindow : EditorWindow
{
    private const string Define = "YUS_DOTWEEN";

    [MenuItem(YusGameFrameEditorMenu.Root + "Systems/Tween/DOTween封装/打开启用窗口")]
    public static void Open()
    {
        GetWindow<YusDotweenWrapperWindow>("Yus DOTween 封装");
    }

    private void OnGUI()
    {
        GUILayout.Label("Yus DOTween 封装系统", EditorStyles.boldLabel);

        bool dotweenInstalled = IsDotweenInstalled();
        bool enabled = HasDefine();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("DOTween 是否存在", dotweenInstalled ? "是" : "否");
        EditorGUILayout.LabelField($"宏 {Define}", enabled ? "已启用" : "未启用");

        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(!dotweenInstalled))
        {
            if (!enabled)
            {
                if (GUILayout.Button("启用系统（添加宏）"))
                {
                    SetDefine(true);
                }
            }
            else
            {
                if (GUILayout.Button("关闭系统（移除宏）"))
                {
                    SetDefine(false);
                }
            }
        }

        if (!dotweenInstalled)
        {
            EditorGUILayout.HelpBox("当前工程里未检测到 DOTween。\n启用宏会导致编译报错，请先导入/安装 DOTween。", MessageType.Warning);
        }

#if YUS_DOTWEEN
        EditorGUILayout.Space(12);
        GUILayout.Label("场景快速安装", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("把 YusTweenManager 挂到当前场景的 YusSingletonManager 下面（没有就创建一个）。", MessageType.Info);

        if (GUILayout.Button("创建/挂载 TweenManager 到 YusSingletonManager"))
        {
            SetupInActiveScene();
        }
#else
        EditorGUILayout.Space(12);
        EditorGUILayout.HelpBox("启用宏后，这里会出现“创建/挂载 TweenManager”按钮。", MessageType.Info);
#endif
    }

    private static bool IsDotweenInstalled()
    {
        return Type.GetType("DG.Tweening.DOTween, DOTween") != null
               || Type.GetType("DG.Tweening.DOTween, DOTween.Core") != null
               || Type.GetType("DG.Tweening.DOTween, DOTweenPro") != null;
    }

    private static BuildTargetGroup CurrentGroup()
    {
        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        if (group == BuildTargetGroup.Unknown) group = BuildTargetGroup.Standalone;
        return group;
    }

    private static bool HasDefine()
    {
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(CurrentGroup());
        return symbols.Split(';').Any(s => string.Equals(s.Trim(), Define, StringComparison.Ordinal));
    }

    private static void SetDefine(bool enable)
    {
        BuildTargetGroup group = CurrentGroup();
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        var list = symbols.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        bool has = list.Any(s => string.Equals(s, Define, StringComparison.Ordinal));
        if (enable && !has) list.Add(Define);
        if (!enable && has) list.RemoveAll(s => string.Equals(s, Define, StringComparison.Ordinal));

        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
        AssetDatabase.Refresh();
    }

#if YUS_DOTWEEN
    private static void SetupInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("[YusDotweenWrapperWindow] Active scene is invalid.");
            return;
        }

        var singletonManager = FindObjectOfType<YusSingletonManager>();
        if (singletonManager == null)
        {
            var go = new GameObject(nameof(YusSingletonManager));
            singletonManager = go.AddComponent<YusSingletonManager>();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        var tween = singletonManager.GetComponentInChildren<YusTweenManager>(true);
        if (tween == null)
        {
            var go = new GameObject(nameof(YusTweenManager));
            go.transform.SetParent(singletonManager.transform, false);
            tween = go.AddComponent<YusTweenManager>();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        Selection.activeObject = tween.gameObject;
        EditorGUIUtility.PingObject(tween.gameObject);
        Debug.Log("[YusDotweenWrapperWindow] YusTweenManager is ready under YusSingletonManager.");
    }
#endif
}
#endif
