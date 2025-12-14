using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class YusTimerDebugger : EditorWindow
{
    [MenuItem("Tools/Yus Data/J.Timer Debugger")]
    public static void ShowWindow()
    {
        GetWindow<YusTimerDebugger>("Timer Debugger");
    }

    private Vector2 scrollPosition;

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请在运行时使用此工具。", MessageType.Info);
            return;
        }

        if (YusTimer.Instance == null)
        {
            EditorGUILayout.HelpBox("场景中没有 YusTimer 实例。", MessageType.Warning);
            return;
        }

        var timers = YusTimer.Instance.DebugGetActiveTimers();
        
        EditorGUILayout.LabelField($"Active Timers: {timers.Count}", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < timers.Count; i++)
        {
            var timer = timers[i];
            DrawTimerInfo(timer, i);
        }

        EditorGUILayout.EndScrollView();
        
        // 强制重绘以实时更新进度条
        Repaint();
    }

    private void DrawTimerInfo(TimerTask timer, int index)
    {
        EditorGUILayout.BeginVertical("box");
        
        // Header: UID + Progress
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"ID: {timer.UID}", GUILayout.Width(60));
        
        float progress = 0;
        if (timer.Duration > 0)
        {
            progress = Mathf.Clamp01(timer.TimeElapsed / timer.Duration);
        }
        
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{timer.TimeElapsed:F2}s / {timer.Duration:F2}s");
        
        EditorGUILayout.EndHorizontal();

        // Details
        EditorGUILayout.BeginHorizontal();
        
        // Loop Info
        string loopInfo = timer.DebugLoopCount == -1 ? "Infinite" : timer.DebugLoopCount.ToString();
        EditorGUILayout.LabelField($"Loop: {loopInfo}", GUILayout.Width(100));
        
        // Realtime
        EditorGUILayout.LabelField($"RealTime: {timer.DebugUseRealTime}", GUILayout.Width(120));

        // Owner
        if (timer.AutoDestroyOwner != null)
        {
            EditorGUILayout.ObjectField(timer.AutoDestroyOwner, typeof(GameObject), true);
        }
        else
        {
            EditorGUILayout.LabelField("No Owner", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();
        
        // Status
        if (timer.IsPaused)
        {
            EditorGUILayout.HelpBox("Paused", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }
}
