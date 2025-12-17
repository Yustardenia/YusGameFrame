using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class YusCinemachine2DSettings
{
    public const string DefineSymbol = "YUS_CINEMACHINE";

    [MenuItem("YusGameFrame/Camera/Cinemachine 2D/Control Panel")]
    public static void OpenPanel()
    {
        YusCinemachine2DSettingsWindow.ShowWindow();
    }

    [MenuItem("YusGameFrame/Camera/Cinemachine 2D/Enable")]
    public static void Enable()
    {
        if (!HasCinemachineInstalled())
        {
            EditorUtility.DisplayDialog(
                "启用 Cinemachine 2D",
                "当前项目未检测到 Cinemachine。\n请先安装包：com.unity.cinemachine",
                "确定");
            return;
        }

        SetDefineEnabled(true);
        EditorUtility.DisplayDialog(
            "启用 Cinemachine 2D",
            $"已添加脚本宏：{DefineSymbol}\nUnity 将触发脚本重新编译。",
            "确定");
    }

    [MenuItem("YusGameFrame/Camera/Cinemachine 2D/Enable", true)]
    private static bool ValidateEnable()
    {
        return HasCinemachineInstalled() && !IsDefineEnabled();
    }

    [MenuItem("YusGameFrame/Camera/Cinemachine 2D/Disable")]
    public static void Disable()
    {
        SetDefineEnabled(false);
        EditorUtility.DisplayDialog(
            "禁用 Cinemachine 2D",
            $"已移除脚本宏：{DefineSymbol}\nUnity 将触发脚本重新编译。",
            "确定");
    }

    [MenuItem("YusGameFrame/Camera/Cinemachine 2D/Disable", true)]
    private static bool ValidateDisable()
    {
        return IsDefineEnabled();
    }

    public static bool HasCinemachineInstalled()
    {
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm == null) continue;
                if (asm.GetType("Cinemachine.CinemachineBrain", false) != null) return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static bool IsDefineEnabled(BuildTargetGroup group = BuildTargetGroup.Unknown)
    {
        if (group == BuildTargetGroup.Unknown)
        {
            group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown) group = BuildTargetGroup.Standalone;
        }

        var current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        return SplitDefines(current).Contains(DefineSymbol);
    }

    public static void SetDefineEnabled(bool enabled, BuildTargetGroup group = BuildTargetGroup.Unknown)
    {
        if (group == BuildTargetGroup.Unknown)
        {
            group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown) group = BuildTargetGroup.Standalone;
        }

        var current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        var set = new HashSet<string>(SplitDefines(current));

        if (enabled) set.Add(DefineSymbol);
        else set.Remove(DefineSymbol);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", set.OrderBy(s => s)));
    }

    private static IEnumerable<string> SplitDefines(string defines)
    {
        if (string.IsNullOrWhiteSpace(defines)) yield break;

        var parts = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var s = p.Trim();
            if (!string.IsNullOrEmpty(s)) yield return s;
        }
    }
}

public sealed class YusCinemachine2DSettingsWindow : EditorWindow
{
    public static void ShowWindow()
    {
        var win = GetWindow<YusCinemachine2DSettingsWindow>("Yus Cinemachine 2D 设置");
        win.minSize = new Vector2(360, 180);
        win.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Cinemachine 2D 封装", EditorStyles.boldLabel);

        bool hasCinemachine = YusCinemachine2DSettings.HasCinemachineInstalled();
        bool enabled = YusCinemachine2DSettings.IsDefineEnabled();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("已安装 Cinemachine", hasCinemachine ? "是" : "否");
        EditorGUILayout.LabelField("已启用宏", enabled ? "是" : "否");
        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(!hasCinemachine || enabled))
        {
            if (GUILayout.Button("启用（添加宏并重新编译）", GUILayout.Height(26)))
            {
                YusCinemachine2DSettings.Enable();
            }
        }

        using (new EditorGUI.DisabledScope(!enabled))
        {
            if (GUILayout.Button("禁用（移除宏并重新编译）", GUILayout.Height(26)))
            {
                YusCinemachine2DSettings.Disable();
            }
        }

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(!enabled))
        {
            if (GUILayout.Button("一键创建 2D Cinemachine Rig", GUILayout.Height(26)))
            {
                YusCamera2DSetupMenu.Setup2DRig();
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "只有启用宏后，本系统才会参与编译。\n" +
            "菜单：YusGameFrame/Camera/Cinemachine 2D",
            MessageType.Info);
    }
}
