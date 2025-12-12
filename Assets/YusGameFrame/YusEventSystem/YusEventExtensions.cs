using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件系统扩展：实现自动移除监听
/// </summary>
public static class YusEventExtensions
{
    // 0参数
    public static void YusRegister(this MonoBehaviour mono, string eventName, Action handler)
    {
        YusEventManager.Instance.AddListener(eventName, handler);
        mono.GetCleaner().AddRecord(() => YusEventManager.Instance.RemoveListener(eventName, handler));
    }

    // 1参数
    public static void YusRegister<T>(this MonoBehaviour mono, string eventName, Action<T> handler)
    {
        YusEventManager.Instance.AddListener(eventName, handler);
        mono.GetCleaner().AddRecord(() => YusEventManager.Instance.RemoveListener(eventName, handler));
    }

    // 2参数
    public static void YusRegister<T1, T2>(this MonoBehaviour mono, string eventName, Action<T1, T2> handler)
    {
        YusEventManager.Instance.AddListener(eventName, handler);
        mono.GetCleaner().AddRecord(() => YusEventManager.Instance.RemoveListener(eventName, handler));
    }

    // 获取或创建清理器组件
    private static YusEventAutoCleaner GetCleaner(this MonoBehaviour mono)
    {
        var cleaner = mono.GetComponent<YusEventAutoCleaner>();
        if (cleaner == null)
        {
            cleaner = mono.gameObject.AddComponent<YusEventAutoCleaner>();
            // 隐藏这个组件，不干扰 Inspector
            cleaner.hideFlags = HideFlags.HideInInspector; 
        }
        return cleaner;
    }
}

/// <summary>
/// 挂载在物体上的隐形组件，负责在销毁时退订事件
/// </summary>
public class YusEventAutoCleaner : MonoBehaviour
{
    private List<Action> cleanupActions = new List<Action>();

    public void AddRecord(Action cleanupAction)
    {
        cleanupActions.Add(cleanupAction);
    }

    private void OnDestroy()
    {
        // 如果游戏关闭了，Instance 可能已经销毁，就不需要退订了
        if (YusEventManager.Instance == null) return;

        foreach (var action in cleanupActions)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[YusEventAutoCleaner] 退订失败: {e.Message}");
            }
        }
        cleanupActions.Clear();
    }
}