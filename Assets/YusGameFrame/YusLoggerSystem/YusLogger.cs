using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class YusLogger : MonoBehaviour
{
    public static YusLogger Instance { get; private set; }

    [Serializable]
    public class LogEntry
    {
        public string Time;
        public LogType Type;
        public string Message;
        public string StackTrace;
    }

    [Header("Settings")]
    [SerializeField] private bool captureUnityLogs = true;
    [SerializeField] private int maxLogCount = 2000;

    private readonly List<LogEntry> logs = new List<LogEntry>();
    private readonly StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (captureUnityLogs)
        {
            Application.logMessageReceived += OnUnityLog;
        }
    }

    private void OnDestroy()
    {
        if (captureUnityLogs)
        {
            Application.logMessageReceived -= OnUnityLog;
        }
    }

    private void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        AddLog(condition, type, stackTrace);
    }

    public static void Log(object message)
    {
        if (Instance != null) Instance.LogInternal(message, LogType.Log);
        else Debug.Log(message);
    }

    public static void Warning(object message)
    {
        if (Instance != null) Instance.LogInternal(message, LogType.Warning);
        else Debug.LogWarning(message);
    }

    public static void Error(object message)
    {
        if (Instance != null) Instance.LogInternal(message, LogType.Error);
        else Debug.LogError(message);
    }

    private void LogInternal(object message, LogType type)
    {
        string msg = message != null ? message.ToString() : "null";

        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                Debug.LogError(msg);
                break;
            case LogType.Warning:
                Debug.LogWarning(msg);
                break;
            default:
                Debug.Log(msg);
                break;
        }

        if (!captureUnityLogs)
        {
            AddLog(msg, type);
        }
    }

    private void AddLog(string message, LogType type, string stackTrace = "")
    {
        if (logs.Count >= maxLogCount)
        {
            logs.RemoveAt(0);
        }

        logs.Add(new LogEntry
        {
            Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Type = type,
            Message = message,
            StackTrace = stackTrace
        });
    }

    public void ExportToExcel()
    {
        string folder = Path.Combine(Application.persistentDataPath, "YusLogs");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string fileName = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = Path.Combine(folder, fileName);

        sb.Clear();
        sb.AppendLine("Time,Type,Message,StackTrace");

        foreach (var log in logs)
        {
            string msg = EscapeCSV(log.Message);
            string stack = EscapeCSV(log.StackTrace);
            sb.AppendLine($"{log.Time},{log.Type},{msg},{stack}");
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        Debug.Log($"[YusLogger] 日志已导出到: {path}");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(path);
#endif
    }

    private static string EscapeCSV(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        if (str.Contains(",") || str.Contains("\"") || str.Contains("\n") || str.Contains("\r"))
        {
            str = str.Replace("\"", "\"\"");
            return $"\"{str}\"";
        }
        return str;
    }
}
