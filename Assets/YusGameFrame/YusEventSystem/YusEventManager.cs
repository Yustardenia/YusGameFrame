using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yus 事件中心 (观察者模式) - 修复版
/// </summary>
public class YusEventManager : MonoBehaviour
{
    // --- 单例模式 (由 YusSingletonManager 统一管理) ---
    public static YusEventManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // 存储表
    private Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();

    // ==========================================
    // 1. 添加监听 (AddListener)
    // ==========================================
    public void AddListener(string eventName, Action handler) 
    { 
        OnAdding(eventName, handler); 
        eventTable[eventName] = (Action)eventTable[eventName] + handler; 
    }
    
    public void AddListener<T>(string eventName, Action<T> handler) 
    { 
        OnAdding(eventName, handler); 
        eventTable[eventName] = (Action<T>)eventTable[eventName] + handler; 
    }
    
    public void AddListener<T1, T2>(string eventName, Action<T1, T2> handler) 
    { 
        OnAdding(eventName, handler); 
        eventTable[eventName] = (Action<T1, T2>)eventTable[eventName] + handler; 
    }
    
    public void AddListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler) 
    { 
        OnAdding(eventName, handler); 
        eventTable[eventName] = (Action<T1, T2, T3>)eventTable[eventName] + handler; 
    }

    // ==========================================
    // 2. 移除监听 (RemoveListener) - 【核心修复】
    // ==========================================
    
    public void RemoveListener(string eventName, Action handler) 
    { 
        if (CheckRemove(eventName, handler)) 
        {
            eventTable[eventName] = (Action)eventTable[eventName] - handler; 
            OnRemoved(eventName); 
        }
    }

    public void RemoveListener<T>(string eventName, Action<T> handler) 
    { 
        if (CheckRemove(eventName, handler)) 
        {
            eventTable[eventName] = (Action<T>)eventTable[eventName] - handler; 
            OnRemoved(eventName); 
        }
    }

    public void RemoveListener<T1, T2>(string eventName, Action<T1, T2> handler) 
    { 
        if (CheckRemove(eventName, handler)) 
        {
            eventTable[eventName] = (Action<T1, T2>)eventTable[eventName] - handler; 
            OnRemoved(eventName); 
        }
    }

    public void RemoveListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler) 
    { 
        if (CheckRemove(eventName, handler)) 
        {
            eventTable[eventName] = (Action<T1, T2, T3>)eventTable[eventName] - handler; 
            OnRemoved(eventName); 
        }
    }

    // ==========================================
    // 3. 广播事件 (Broadcast)
    // ==========================================
    public void Broadcast(string eventName)
    {
#if UNITY_EDITOR
        LogHistory(eventName);
#endif
        if (eventTable.TryGetValue(eventName, out Delegate d))
        {
            if (d is Action callback) callback.Invoke();
            // else Debug.LogError($"[YusEvent] 事件 {eventName} 参数不匹配 (应无参数)"); // 暂时屏蔽报错，防止滥用
        }
    }

    public void Broadcast<T>(string eventName, T arg1)
    {
#if UNITY_EDITOR
        LogHistory(eventName);
#endif
        if (eventTable.TryGetValue(eventName, out Delegate d))
        {
            if (d is Action<T> callback) callback.Invoke(arg1);
        }
    }

    public void Broadcast<T1, T2>(string eventName, T1 arg1, T2 arg2)
    {
#if UNITY_EDITOR
        LogHistory(eventName);
#endif
        if (eventTable.TryGetValue(eventName, out Delegate d))
        {
            if (d is Action<T1, T2> callback) callback.Invoke(arg1, arg2);
        }
    }

    public void Broadcast<T1, T2, T3>(string eventName, T1 arg1, T2 arg2, T3 arg3)
    {
#if UNITY_EDITOR
        LogHistory(eventName);
#endif
        if (eventTable.TryGetValue(eventName, out Delegate d))
        {
            if (d is Action<T1, T2, T3> callback) callback.Invoke(arg1, arg2, arg3);
        }
    }

    // ==========================================
    // 4. 安全检查辅助
    // ==========================================
    
    private void OnAdding(string eventName, Delegate listener)
    {
        if (!eventTable.ContainsKey(eventName)) 
        {
            eventTable.Add(eventName, null);
        }
        
        Delegate d = eventTable[eventName];
        if (d != null && d.GetType() != listener.GetType())
        {
            YusLogger.Error($"[YusEvent] 尝试为 {eventName} 添加类型不匹配的监听: 当前{d.GetType()}, 新增{listener.GetType()}");
        }
    }

    // 【新增】统一检查是否可以移除，防止 KeyNotFoundException
    private bool CheckRemove(string eventName, Delegate listener)
    {
        if (!eventTable.ContainsKey(eventName)) 
        {
            return false; // 字典里没这个事件，直接忽略，不报错
        }

        Delegate d = eventTable[eventName];
        if (d != null && d.GetType() != listener.GetType())
        {
            YusLogger.Error($"[YusEvent] 移除监听类型不匹配: {eventName}");
            return false;
        }
        return true;
    }

    private void OnRemoved(string eventName)
    {
        if (eventTable.ContainsKey(eventName) && eventTable[eventName] == null)
        {
            eventTable.Remove(eventName);
        }
    }

    // ==========================================
    // 5. 编辑器调试接口
    // ==========================================
#if UNITY_EDITOR
    public class BroadcastRecord { public string time; public string eventName; public string sender; }
    public List<BroadcastRecord> history = new List<BroadcastRecord>();

    private void LogHistory(string name)
    {
        if (history.Count > 50) history.RemoveAt(0);
        string senderInfo = "Code";
        var stack = new System.Diagnostics.StackTrace();
        if (stack.FrameCount > 2)
        {
            var method = stack.GetFrame(2).GetMethod();
            if (method.DeclaringType != null)
                senderInfo = $"{method.DeclaringType.Name}.{method.Name}";
        }
        history.Add(new BroadcastRecord { time = DateTime.Now.ToString("HH:mm:ss"), eventName = name, sender = senderInfo });
    }

    public Dictionary<string, Delegate> GetEventTable() => eventTable;
#endif
}