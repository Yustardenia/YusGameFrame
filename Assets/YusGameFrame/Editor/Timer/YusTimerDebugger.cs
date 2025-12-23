using UnityEditor;
using UnityEngine;

public class YusTimerDebugger : EditorWindow
{
    [MenuItem(YusGameFrameEditorMenu.Root + "Debug/Timer Debugger")]
    public static void ShowWindow()
    {
        GetWindow<YusTimerDebugger>("Timer Debugger");
    }

    private Vector2 scrollPosition;

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use Timer Debugger.", MessageType.Info);
            return;
        }

        if (YusTimer.Instance == null)
        {
            EditorGUILayout.HelpBox("No YusTimer instance in scene.", MessageType.Warning);
            return;
        }

        var timers = YusTimer.Instance.DebugGetActiveTimers();
        EditorGUILayout.LabelField($"Active Timers: {timers.Count}", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < timers.Count; i++)
        {
            DrawTimerInfo(timers[i]);
        }
        EditorGUILayout.EndScrollView();

        Repaint();
    }

    private static void DrawTimerInfo(TimerTask timer)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"ID: {timer.UID}", GUILayout.Width(70));

        float progress = timer.Duration > 0 ? Mathf.Clamp01(timer.TimeElapsed / timer.Duration) : 0f;

        EditorGUI.ProgressBar(
            EditorGUILayout.GetControlRect(),
            progress,
            $"{timer.Mode} | {timer.CurrentTime:F2}s / {timer.Duration:F2}s");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        string loopInfo = timer.DebugLoopCount == -1 ? "Infinite" : timer.DebugLoopCount.ToString();
        EditorGUILayout.LabelField($"Loop: {loopInfo}", GUILayout.Width(100));
        EditorGUILayout.LabelField($"RealTime: {timer.DebugUseRealTime}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"Elapsed: {timer.TimeElapsed:F2}s", GUILayout.Width(120));
        EditorGUILayout.LabelField($"Remain: {timer.RemainingTime:F2}s", GUILayout.Width(120));

        if (timer.AutoDestroyOwner != null)
        {
            EditorGUILayout.ObjectField(timer.AutoDestroyOwner, typeof(GameObject), true);
        }
        else
        {
            EditorGUILayout.LabelField("No Owner", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();

        if (timer.IsPaused)
        {
            EditorGUILayout.HelpBox("Paused", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }
}
