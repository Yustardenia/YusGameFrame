#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class YusLoggerMenu
{
    [MenuItem(YusGameFrameEditorMenu.Root + "Debug/导出当前日志 (Export Logs)")]
    private static void ExportLogs()
    {
        if (YusLogger.Instance != null)
        {
            YusLogger.Instance.ExportToExcel();
        }
        else
        {
            Debug.LogWarning("YusLogger is not running, cannot export in-memory logs. Please enter Play Mode first.");
        }
    }
}
#endif
