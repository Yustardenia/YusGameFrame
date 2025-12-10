using UnityEngine;
using System.Collections.Generic;

public class YusFSM<T>
{
    public T Owner { get; private set; }
    public YusState<T> CurrentState { get; private set; }
    public YusState<T> PreviousState { get; private set; } // 记录上一个状态，方便“返回”

    // 状态缓存池 (避免频繁 new 产生 GC)
    private Dictionary<System.Type, YusState<T>> stateCache = new Dictionary<System.Type, YusState<T>>();

    public YusFSM(T owner)
    {
        this.Owner = owner;
    }

    // --- 启动状态机 ---
    public void Start<TState>() where TState : YusState<T>, new()
    {
        ChangeState<TState>();
    }

    // --- 切换状态 (核心) ---
    public void ChangeState<TState>() where TState : YusState<T>, new()
    {
        // 1. 获取或创建状态实例
        System.Type type = typeof(TState);
        if (!stateCache.TryGetValue(type, out YusState<T> newState))
        {
            newState = new TState();
            newState.Init(Owner, this);
            stateCache.Add(type, newState);
        }

        // 2. 如果是同一个状态，是否允许重复进入？(通常不)
        if (CurrentState == newState) return;

        // 3. 退出旧状态
        if (CurrentState != null)
        {
            PreviousState = CurrentState;
            CurrentState.OnExit();
        }

        // 4. 进入新状态
        CurrentState = newState;
        CurrentState.OnEnter();
        
        // (可选) 调试日志
        // Debug.Log($"[FSM] {Owner.GetType().Name} 切换状态: {type.Name}");
    }

    // --- 返回上一个状态 ---
    public void RevertState()
    {
        if (PreviousState != null)
        {
            // 这里不能直接 ChangeState<T> 因为 PreviousState 是实例
            // 我们手动执行切换逻辑
            CurrentState.OnExit();
            
            var temp = CurrentState;
            CurrentState = PreviousState;
            PreviousState = temp; // 交换历史，方便反复横跳

            CurrentState.OnEnter();
        }
    }

    // --- 生命周期驱动 (必须在 Owner 的 Update 中调用) ---
    public void OnUpdate()
    {
        CurrentState?.OnUpdate();
    }

    public void OnFixedUpdate()
    {
        CurrentState?.OnFixedUpdate();
    }
    
    // --- 销毁清理 ---
    public void Stop()
    {
        CurrentState?.OnExit();
        CurrentState = null;
        PreviousState = null;
        stateCache.Clear();
    }
#if UNITY_EDITOR
    public Dictionary<System.Type, YusState<T>> Debug_GetStateCache()
    {
        return stateCache;
    }
#endif
}

