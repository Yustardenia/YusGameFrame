using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 计时器管理器
/// 负责驱动所有计时器的更新，并提供对象池管理
/// </summary>
public class YusTimer : MonoBehaviour
{
    public static YusTimer Instance { get; private set; }

    // 活动中的计时器列表
    private List<TimerTask> activeTimers = new List<TimerTask>();
#if UNITY_EDITOR
    public List<TimerTask> DebugGetActiveTimers() => activeTimers;
#endif
    
    // 待添加的计时器（防止在Update循环中添加导致集合修改异常）
    private List<TimerTask> timersToAdd = new List<TimerTask>();

    // 计时器对象池
    private Stack<TimerTask> timerPool = new Stack<TimerTask>();

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
        }
    }

    private void Update()
    {
        // 1. 添加新注册的计时器
        if (timersToAdd.Count > 0)
        {
            activeTimers.AddRange(timersToAdd);
            timersToAdd.Clear();
        }

        // 2. 更新所有活动计时器
        // 使用倒序遍历，方便移除完成的计时器
        for (int i = activeTimers.Count - 1; i >= 0; i--)
        {
            var timer = activeTimers[i];

            // 如果绑定了 GameObject 且该对象已销毁，则自动停止计时器
            // 必须强转 object 判空来确认是否"曾经绑定过"，因为对于已销毁的 Unity 对象，(obj != null) 会返回 false
            if (((object)timer.AutoDestroyOwner) != null && timer.AutoDestroyOwner == null)
            {
                timer.Cancel();
            }

            if (timer.IsDone)
            {
                Recycle(timer);
                activeTimers.RemoveAt(i);
                continue;
            }

            timer.Tick(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }

    /// <summary>
    /// 创建一个计时器
    /// </summary>
    public static TimerTask Create(float duration, Action onComplete = null)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("YusTimer");
            go.AddComponent<YusTimer>();
        }

        TimerTask timer = Instance.GetFromPool();
        timer.Initialize(duration, onComplete);
        Instance.timersToAdd.Add(timer);
        return timer;
    }

    private TimerTask GetFromPool()
    {
        if (timerPool.Count > 0)
        {
            return timerPool.Pop();
        }
        return new TimerTask();
    }

    private void Recycle(TimerTask timer)
    {
        timer.Reset();
        timerPool.Push(timer);
    }
}

/// <summary>
/// 单个计时器任务
/// 支持链式调用配置
/// </summary>
public class TimerTask
{
    // 唯一ID，用于解决对象池引用问题
    private static int _globalIdCounter;
    public int UID { get; private set; }

    // 基础属性
    public float Duration { get; private set; }
    public float TimeElapsed { get; private set; }
    public bool IsDone { get; private set; }
    public bool IsPaused { get; private set; }
    
    // 配置
    private bool useRealTime;
    private int loopCount; // 0=1次, -1=无限, >0=指定次数
    private GameObject autoDestroyOwner; // 绑定的所有者

#if UNITY_EDITOR
    public bool DebugUseRealTime => useRealTime;
    public int DebugLoopCount => loopCount;
#endif

    // 回调
    private Action onComplete;
    private Action<float> onUpdate;

    public GameObject AutoDestroyOwner => autoDestroyOwner;

    public TimerTask() { }

    public void Initialize(float duration, Action onComplete)
    {
        this.UID = ++_globalIdCounter;
        this.Duration = duration;
        this.onComplete = onComplete;
        this.TimeElapsed = 0;
        this.IsDone = false;
        this.IsPaused = false;
        this.loopCount = 0;
        this.useRealTime = false;
        this.autoDestroyOwner = null;
        this.onUpdate = null;
    }

    public void Reset()
    {
        onComplete = null;
        onUpdate = null;
        autoDestroyOwner = null;
        // UID 不重置，保持唯一性直到下次 Initialize
    }

    public void Tick(float deltaTime, float unscaledDeltaTime)
    {
        if (IsDone || IsPaused) return;

        float dt = useRealTime ? unscaledDeltaTime : deltaTime;
        TimeElapsed += dt;

        // 执行 Update 回调 (传入剩余时间)
        if (onUpdate != null)
        {
            onUpdate.Invoke(Mathf.Max(0, Duration - TimeElapsed));
        }

        if (TimeElapsed >= Duration)
        {
            // 触发完成回调
            onComplete?.Invoke();

            if (loopCount == -1) // 无限循环
            {
                TimeElapsed = 0;
            }
            else if (loopCount > 0) // 有限循环
            {
                loopCount--;
                TimeElapsed = 0;
            }
            else // 完成
            {
                IsDone = true;
            }
        }
    }

    // ================= API =================

    /// <summary>
    /// 暂停
    /// </summary>
    public TimerTask Pause()
    {
        IsPaused = true;
        return this;
    }

    /// <summary>
    /// 恢复
    /// </summary>
    public TimerTask Resume()
    {
        IsPaused = false;
        return this;
    }

    /// <summary>
    /// 取消（立即停止并标记为完成，下一帧会被回收）
    /// </summary>
    public void Cancel()
    {
        IsDone = true;
    }

    /// <summary>
    /// 设置循环
    /// </summary>
    /// <param name="count">-1 为无限循环，>0 为额外循环次数</param>
    public TimerTask SetLoop(int count)
    {
        this.loopCount = count;
        return this;
    }

    /// <summary>
    /// 设置是否使用真实时间（不受 Time.timeScale 影响）
    /// </summary>
    public TimerTask SetUseRealTime(bool useRealTime)
    {
        this.useRealTime = useRealTime;
        return this;
    }

    /// <summary>
    /// 设置 Update 回调
    /// </summary>
    /// <param name="onUpdate">参数为剩余时间</param>
    public TimerTask OnUpdate(Action<float> onUpdate)
    {
        this.onUpdate = onUpdate;
        return this;
    }

    /// <summary>
    /// 绑定到 GameObject，当该对象销毁时，计时器自动取消
    /// </summary>
    public TimerTask Attach(GameObject owner)
    {
        this.autoDestroyOwner = owner;
        return this;
    }
}

/// <summary>
/// 计时器容器
/// 用于在某个类中管理一组计时器，方便统一销毁
/// </summary>
public class TimerContainer
{
    // 存储 计时器引用 + 当时的UID，用于验证计时器是否有效（防止对象池复用导致引用错误）
    private List<(TimerTask task, int uid)> timers = new List<(TimerTask, int)>();
    private Dictionary<string, (TimerTask task, int uid)> namedTimers = new Dictionary<string, (TimerTask, int)>();

    /// <summary>
    /// 创建并添加一个计时器到此容器
    /// </summary>
    public TimerTask AddTimer(float duration, Action onComplete = null)
    {
        return AddTimer(null, duration, onComplete);
    }

    /// <summary>
    /// 创建并添加一个带名称的计时器到此容器
    /// 如果同名计时器已存在，会先停止旧的
    /// </summary>
    public TimerTask AddTimer(string name, float duration, Action onComplete = null)
    {
        // 1. 清理无效或已完成的计时器
        for (int i = timers.Count - 1; i >= 0; i--)
        {
            var (t, uid) = timers[i];
            // 如果UID不匹配（说明被回收复用了）或者已完成，则移除
            if (t.UID != uid || t.IsDone)
            {
                timers.RemoveAt(i);
            }
        }

        // 2. 处理同名冲突
        if (!string.IsNullOrEmpty(name))
        {
            if (namedTimers.TryGetValue(name, out var pair))
            {
                // 如果旧计时器仍然有效且未完成，取消它
                if (pair.task.UID == pair.uid && !pair.task.IsDone)
                {
                    pair.task.Cancel();
                }
                namedTimers.Remove(name);
            }
        }

        // 3. 创建新计时器
        TimerTask timer = YusTimer.Create(duration, onComplete);
        var timerRecord = (timer, timer.UID);
        
        timers.Add(timerRecord);

        if (!string.IsNullOrEmpty(name))
        {
            namedTimers[name] = timerRecord;
        }

        return timer;
    }

    /// <summary>
    /// 获取指定名称的计时器
    /// </summary>
    public TimerTask GetTimer(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        if (namedTimers.TryGetValue(name, out var pair))
        {
            // 验证UID和状态
            if (pair.task.UID == pair.uid && !pair.task.IsDone)
            {
                return pair.task;
            }
            else
            {
                // 失效了，移除
                namedTimers.Remove(name);
            }
        }
        return null;
    }

    /// <summary>
    /// 停止并清理所有计时器
    /// </summary>
    public void Clear()
    {
        foreach (var (t, uid) in timers)
        {
            // 仅取消仍然属于我们的有效计时器
            if (t.UID == uid && !t.IsDone)
            {
                t.Cancel();
            }
        }
        timers.Clear();
        namedTimers.Clear();
    }
}
