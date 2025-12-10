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
        mono.GetCleaner().AddRecord(eventName, handler);
    }

    // 1参数
    public static void YusRegister<T>(this MonoBehaviour mono, string eventName, Action<T> handler)
    {
        YusEventManager.Instance.AddListener(eventName, handler);
        mono.GetCleaner().AddRecord(eventName, handler);
    }

    // 2参数
    public static void YusRegister<T1, T2>(this MonoBehaviour mono, string eventName, Action<T1, T2> handler)
    {
        YusEventManager.Instance.AddListener(eventName, handler);
        mono.GetCleaner().AddRecord(eventName, handler);
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
/// <summary>
/// 挂载在物体上的隐形组件，负责在销毁时退订事件
/// </summary>
public class YusEventAutoCleaner : MonoBehaviour
{
    private List<(string name, Delegate handler)> records = new List<(string, Delegate)>();

    public void AddRecord(string name, Delegate handler)
    {
        records.Add((name, handler));
    }

    private void OnDestroy()
    {
        // 如果游戏关闭了，Instance 可能已经销毁，就不需要退订了
        if (YusEventManager.Instance == null) return;

        // 遍历所有记录，通过反射找到对应的 RemoveListener 方法并调用
        foreach (var record in records)
        {
            string eventName = record.name;
            Delegate handler = record.handler;
            Type handlerType = handler.GetType();

            // 情况 1: 无参数 Action
            if (handler is Action action)
            {
                YusEventManager.Instance.RemoveListener(eventName, action);
                continue;
            }

            // 情况 2: 泛型参数 (Action<T>, Action<T1, T2>...)
            if (handlerType.IsGenericType)
            {
                // 获取委托的泛型参数 (例如 int, string)
                Type[] genericArgs = handlerType.GetGenericArguments();

                // 在 Manager 中查找名字叫 RemoveListener 的方法
                var methods = typeof(YusEventManager).GetMethods();
                foreach (var method in methods)
                {
                    if (method.Name == "RemoveListener" && method.IsGenericMethodDefinition)
                    {
                        // 检查泛型参数数量是否一致
                        // 例如 RemoveListener<T> 有1个参数，Action<int> 也有1个参数
                        if (method.GetGenericArguments().Length == genericArgs.Length)
                        {
                            // 构造具体的方法: RemoveListener<int>
                            var specificMethod = method.MakeGenericMethod(genericArgs);
                            
                            // 调用方法: RemoveListener<int>(eventName, handler)
                            specificMethod.Invoke(YusEventManager.Instance, new object[] { eventName, handler });
                            break;
                        }
                    }
                }
            }
        }
        records.Clear();
    }
}