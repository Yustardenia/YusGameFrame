using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class YusLogger : MonoBehaviour
{
    public static YusLogger Instance { get; private set; }

    [System.Serializable]
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

    private List<LogEntry> logs = new List<LogEntry>();
    private StringBuilder sb = new StringBuilder();

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

    // --- 静态便捷接口 (Static API) ---
    
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

    // --- 内部实现 ---

    private void LogInternal(object message, LogType type)
    {
        string msg = message?.ToString() ?? "null";
        
        // 1. 打印到 Unity 控制台
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

        // 2. 如果没有开启自动捕获，则手动记录到列表
        // (如果开启了自动捕获，Debug.Log 会触发 OnUnityLog，从而调用 AddLog)
        if (!captureUnityLogs)
        {
            AddLog(msg, type);
        }
    }

    private void AddLog(string message, LogType type, string stackTrace = "")
    {
        // 限制内存中日志数量，防止爆内存
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

    /// <summary>
    /// 导出日志到 CSV (Excel 可直接打开)
    /// </summary>
    public void ExportToExcel()
    {
        string folder = Path.Combine(Application.persistentDataPath, "YusLogs");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string fileName = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = Path.Combine(folder, fileName);

        sb.Clear();
        // CSV Header (Excel 会自动识别逗号分隔)
        sb.AppendLine("Time,Type,Message,StackTrace");

        foreach (var log in logs)
        {
            // 处理 CSV 特殊字符 (逗号、引号、换行)
            string msg = EscapeCSV(log.Message);
            string stack = EscapeCSV(log.StackTrace);
            
            sb.AppendLine($"{log.Time},{log.Type},{msg},{stack}");
        }

        // 使用 UTF8 BOM 编码，防止 Excel 打开中文乱码
        // new UTF8Encoding(true) 会带上 BOM
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        Debug.Log($"[YusLogger] 日志已导出到: {path}");
        
#if UNITY_EDITOR
        EditorUtility.RevealInFinder(path);
#endif
    }

    private string EscapeCSV(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        // 如果包含逗号、引号或换行，需要用双引号包裹，并将内部的双引号转义为两个双引号
        if (str.Contains(",") || str.Contains("\"") || str.Contains("\n") || str.Contains("\r"))
        {
            str = str.Replace("\"", "\"\"");
            return $"\"{str}\"";
        }
        return str;
    }
    
#if UNITY_EDITOR
    [MenuItem("Tools/Yus Data/10. 导出当前日志 (Export Logs)")]
    public static void MenuExport()
    {
        if (Instance != null)
        {
            Instance.ExportToExcel();
        }
        else
        {
            Debug.LogWarning("YusLogger 运行时未启动，无法导出内存日志。请先运行游戏。");
        }
    }
#endif
}
