#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class YusCoroutineDebuggerWindow : EditorWindow
{
    private const double RefreshIntervalSeconds = 0.25d;

    private string _search = "";
    private bool _onlyWithOwner = false;
    private bool _onlyWithTag = false;
    private Vector2 _scroll;
    private double _nextRefreshAt;

    [MenuItem(YusGameFrameEditorMenu.Root + "Debug/协程调试器")]
    public static void Open()
    {
        GetWindow<YusCoroutineDebuggerWindow>("Coroutine Debugger");
    }

    private void OnEnable()
    {
        _nextRefreshAt = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (EditorApplication.timeSinceStartup < _nextRefreshAt) return;
        _nextRefreshAt = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Coroutine Debugger only works in Play Mode.", MessageType.Info);
            return;
        }

        var mgr = YusCoroutineManager.Instance != null ? YusCoroutineManager.Instance : FindObjectOfType<YusCoroutineManager>();
        if (mgr == null)
        {
            EditorGUILayout.HelpBox("No YusCoroutineManager found in the scene.", MessageType.Warning);
            if (GUILayout.Button("Create Manager"))
            {
                var go = new GameObject(nameof(YusCoroutineManager));
                go.AddComponent<YusCoroutineManager>();
                Selection.activeGameObject = go;
            }
            return;
        }

        List<YusCoroutineManager.DebugTaskInfo> tasks = mgr.DebugGetTasksSnapshot();
        tasks.Sort((a, b) => b.AgeSeconds.CompareTo(a.AgeSeconds));

        var filtered = ApplyFilter(tasks);

        EditorGUILayout.LabelField($"Active: {tasks.Count}   Showing: {filtered.Count}", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Stop All", GUILayout.Width(90)))
            {
                YusCoroutineManager.StopAll();
            }

            if (GUILayout.Button("Clear Search", GUILayout.Width(100)))
            {
                _search = "";
            }
        }

        EditorGUILayout.Space(4);

        DrawHeaderRow();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < filtered.Count; i++)
        {
            DrawTaskRow(filtered[i]);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Search", GUILayout.Width(44));
            _search = GUILayout.TextField(_search ?? "", EditorStyles.toolbarTextField, GUILayout.MinWidth(140));

            GUILayout.Space(8);
            _onlyWithOwner = GUILayout.Toggle(_onlyWithOwner, "Owner", EditorStyles.toolbarButton, GUILayout.Width(56));
            _onlyWithTag = GUILayout.Toggle(_onlyWithTag, "Tag", EditorStyles.toolbarButton, GUILayout.Width(44));

            GUILayout.FlexibleSpace();
            GUILayout.Label(EditorApplication.isPlaying ? "Play" : "Edit", EditorStyles.miniLabel, GUILayout.Width(32));
        }
    }

    private List<YusCoroutineManager.DebugTaskInfo> ApplyFilter(List<YusCoroutineManager.DebugTaskInfo> tasks)
    {
        IEnumerable<YusCoroutineManager.DebugTaskInfo> q = tasks;

        if (!string.IsNullOrWhiteSpace(_search))
        {
            string s = _search.Trim();
            q = q.Where(t =>
                ContainsIgnoreCase(t.Tag, s) ||
                ContainsIgnoreCase(t.OwnerName, s) ||
                ContainsIgnoreCase(t.OwnerType, s) ||
                t.Id.ToString().Contains(s));
        }

        if (_onlyWithOwner)
        {
            q = q.Where(t => !string.Equals(t.OwnerName, "<None>", StringComparison.Ordinal));
        }

        if (_onlyWithTag)
        {
            q = q.Where(t => !string.IsNullOrEmpty(t.Tag));
        }

        return q.ToList();
    }

    private static bool ContainsIgnoreCase(string src, string needle)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(needle)) return false;
        return src.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void DrawHeaderRow()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Id", GUILayout.Width(56));
            GUILayout.Label("Age", GUILayout.Width(68));
            GUILayout.Label("Tag", GUILayout.Width(160));
            GUILayout.Label("Owner", GUILayout.MinWidth(220));
            GUILayout.FlexibleSpace();
            GUILayout.Label("", GUILayout.Width(66));
        }
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    private static void DrawTaskRow(YusCoroutineManager.DebugTaskInfo task)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(task.Id.ToString(), GUILayout.Width(56));
            GUILayout.Label($"{task.AgeSeconds:0.00}s", GUILayout.Width(68));
            GUILayout.Label(string.IsNullOrEmpty(task.Tag) ? "-" : task.Tag, GUILayout.Width(160));

            string owner = $"{task.OwnerType} / {task.OwnerName}";
            if (task.OwnerDestroyed) owner = $"<Destroyed> / {task.OwnerName}";
            GUILayout.Label(owner, GUILayout.MinWidth(220));

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(task.Id == 0))
            {
                if (GUILayout.Button("Stop", GUILayout.Width(66)))
                {
                    YusCoroutineManager.Stop(task.Id);
                }
            }
        }
    }
}
#endif
