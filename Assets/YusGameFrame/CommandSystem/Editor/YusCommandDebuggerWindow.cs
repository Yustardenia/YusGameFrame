#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class YusCommandDebuggerWindow : EditorWindow
{
    private const double RefreshIntervalSeconds = 0.25d;

    private string _search = "";
    private Vector2 _scroll;
    private double _nextRefreshAt;
    private bool _showLog = true;
    private bool _showRegistry = true;
    private bool _showStacks = true;

    [MenuItem(YusGameFrameEditorMenu.Root + "Debug/Command System")]
    public static void Open()
    {
        GetWindow<YusCommandDebuggerWindow>("Command System");
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

        DrawHeader();

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Undo", GUILayout.Width(72)))
                    YusGameFrame.YusCommand.TryUndo();

                if (GUILayout.Button("Redo", GUILayout.Width(72)))
                    YusGameFrame.YusCommand.TryRedo();
            }

            if (GUILayout.Button("Clear History", GUILayout.Width(110)))
                YusGameFrame.YusCommand.ClearHistory();

            if (GUILayout.Button("Clear Log", GUILayout.Width(90)))
                YusGameFrame.YusCommand.ClearLog();

            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(6);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_showRegistry) DrawRegistry();
        if (_showStacks) DrawStacks();
        if (_showLog) DrawLog();
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Search", GUILayout.Width(44));
            _search = GUILayout.TextField(_search ?? "", EditorStyles.toolbarTextField, GUILayout.MinWidth(160));

            GUILayout.Space(8);
            _showRegistry = GUILayout.Toggle(_showRegistry, "Registry", EditorStyles.toolbarButton, GUILayout.Width(64));
            _showStacks = GUILayout.Toggle(_showStacks, "Stacks", EditorStyles.toolbarButton, GUILayout.Width(56));
            _showLog = GUILayout.Toggle(_showLog, "Log", EditorStyles.toolbarButton, GUILayout.Width(44));

            GUILayout.FlexibleSpace();
            GUILayout.Label(EditorApplication.isPlaying ? "Play" : "Edit", EditorStyles.miniLabel, GUILayout.Width(32));
        }
    }

    private static void DrawHeader()
    {
        var sys = YusGameFrame.YusCommand.System;
        string sysType = sys != null ? sys.GetType().Name : "<null>";
        EditorGUILayout.LabelField($"System: {sysType}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Undo: {YusGameFrame.YusCommand.UndoCount}   Redo: {YusGameFrame.YusCommand.RedoCount}");
    }

    private void DrawRegistry()
    {
        EditorGUILayout.LabelField("Registry", EditorStyles.boldLabel);

        var sys = YusGameFrame.YusCommand.System as YusGameFrame.YusCommandSystem;
        if (sys == null)
        {
            IReadOnlyList<string> keys = YusGameFrame.YusCommand.GetRegisteredKeysSnapshot();
            if (keys.Count == 0)
            {
                EditorGUILayout.HelpBox("No registered commands. Call YusCommand.Register(key, factory) from your systems.", MessageType.Info);
                EditorGUILayout.Space(4);
                return;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (!PassSearch(key)) continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(key, GUILayout.MinWidth(240));
                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                    {
                        if (GUILayout.Button("Execute", GUILayout.Width(70)))
                            YusGameFrame.YusCommand.TryExecute(key);
                    }
                }
            }

            EditorGUILayout.Space(4);
            return;
        }

        var entries = sys.DebugGetRegistrySnapshot();
        if (entries.Length == 0)
        {
            EditorGUILayout.HelpBox("No registered commands. Call YusCommand.Register/RegisterAsync from your systems.", MessageType.Info);
            EditorGUILayout.Space(4);
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (!PassSearch(e.Key) && !PassSearch(e.ArgTypeFullName) && !PassSearch(e.IsAsync ? "Async" : "Sync"))
                continue;

            using (new EditorGUILayout.HorizontalScope())
            {
                var kind = e.IsAsync ? "Async" : "Sync";
                var arg = string.IsNullOrEmpty(e.ArgTypeFullName) ? "()" : $"({ShortTypeName(e.ArgTypeFullName)})";
                GUILayout.Label($"{e.Key}   {kind} {arg}", GUILayout.MinWidth(320));
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                {
                    if (GUILayout.Button("Execute", GUILayout.Width(70)))
                    {
                        if (e.IsAsync)
                            _ = YusGameFrame.YusCommand.TryExecuteAsync(e.Key);
                        else
                            YusGameFrame.YusCommand.TryExecute(e.Key);
                    }
                }
            }
        }

        EditorGUILayout.Space(8);
    }

    private void DrawStacks()
    {
        EditorGUILayout.LabelField("Stacks", EditorStyles.boldLabel);

        var sys = YusGameFrame.YusCommand.System as YusGameFrame.YusCommandSystem;
        if (sys == null)
        {
            EditorGUILayout.HelpBox("Stacks view is only available when YusCommand.System is YusCommandSystem.", MessageType.Info);
            EditorGUILayout.Space(8);
            return;
        }

        var invoker = sys.DebugInvoker;
        var undo = invoker.DebugGetUndoSnapshot();
        var redo = invoker.DebugGetRedoSnapshot();

        DrawStack("Undo (top first)", undo);
        DrawStack("Redo (top first)", redo);

        EditorGUILayout.Space(8);
    }

    private void DrawStack(string title, YusGameFrame.IUndoableCommand[] stack)
    {
        EditorGUILayout.LabelField($"{title}: {stack.Length}");
        if (stack.Length == 0)
            return;

        var maxShow = Mathf.Min(stack.Length, 20);
        for (int i = 0; i < maxShow; i++)
        {
            var cmd = stack[i];
            string name = GetCommandName(cmd);
            string type = cmd != null ? cmd.GetType().Name : "<null>";
            if (!PassSearch(name) && !PassSearch(type)) continue;
            EditorGUILayout.LabelField($"  {i + 1}. {name} ({type})");
        }

        if (stack.Length > maxShow)
            EditorGUILayout.LabelField($"  ... {stack.Length - maxShow} more");
    }

    private void DrawLog()
    {
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);

        var logs = YusGameFrame.YusCommand.LogEntries;
        if (logs.Count == 0)
        {
            EditorGUILayout.HelpBox("No log entries yet.", MessageType.Info);
            return;
        }

        for (int i = logs.Count - 1; i >= 0; i--)
        {
            var e = logs[i];
            if (!PassSearch(e.Name) && !PassSearch(e.CommandType) && !PassSearch(e.Message) && !PassSearch(e.Kind.ToString()))
                continue;

            EditorGUILayout.LabelField(e.ToString(), EditorStyles.miniLabel);
        }
    }

    private bool PassSearch(string s)
    {
        if (string.IsNullOrWhiteSpace(_search)) return true;
        if (string.IsNullOrEmpty(s)) return false;
        return s.IndexOf(_search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetCommandName(YusGameFrame.ICommand command)
    {
        if (command is YusGameFrame.INamedCommand named && !string.IsNullOrWhiteSpace(named.Name))
            return named.Name;

        return command != null ? command.GetType().Name : "<null>";
    }

    private static string ShortTypeName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return fullName;
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
    }
}
#endif
