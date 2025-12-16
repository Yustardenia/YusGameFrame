using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct YusCoroutineHandle : IEquatable<YusCoroutineHandle>
{
    public int Id { get; }

    internal YusCoroutineHandle(int id)
    {
        Id = id;
    }

    public bool IsValid => Id != 0 && YusCoroutineManager.IsRunning(Id);

    public void Stop()
    {
        YusCoroutineManager.Stop(Id);
    }

    public bool Equals(YusCoroutineHandle other) => Id == other.Id;
    public override bool Equals(object obj) => obj is YusCoroutineHandle other && Equals(other);
    public override int GetHashCode() => Id;
    public override string ToString() => $"YusCoroutineHandle({Id})";
}

public static class YusCoroutine
{
    public static YusCoroutineHandle Run(IEnumerator routine, UnityEngine.Object owner = null, string tag = null)
    {
        return YusCoroutineManager.Run(routine, owner, tag);
    }

    public static YusCoroutineHandle Delay(
        float seconds,
        Action action,
        UnityEngine.Object owner = null,
        bool unscaledTime = false,
        string tag = null)
    {
        return YusCoroutineManager.Delay(seconds, action, owner, unscaledTime, tag);
    }

    public static YusCoroutineHandle NextFrame(Action action, UnityEngine.Object owner = null, string tag = null)
    {
        return YusCoroutineManager.NextFrame(action, owner, tag);
    }

    public static YusCoroutineHandle Repeat(
        float interval,
        Action action,
        int repeatCount = -1,
        float firstDelay = 0f,
        UnityEngine.Object owner = null,
        bool unscaledTime = false,
        string tag = null)
    {
        return YusCoroutineManager.Repeat(interval, action, repeatCount, firstDelay, owner, unscaledTime, tag);
    }

    public static int StopTag(string tag) => YusCoroutineManager.StopTag(tag);
    public static int StopOwner(UnityEngine.Object owner) => YusCoroutineManager.StopOwner(owner);
    public static void StopAll() => YusCoroutineManager.StopAll();
}

/// <summary>
/// Coroutine manager:
/// - Can start coroutines without needing a MonoBehaviour.
/// - Supports owner-binding (auto stops when Unity Object is destroyed).
/// - Supports tag/owner stop and returning a handle for manual cancel.
/// </summary>
public class YusCoroutineManager : MonoBehaviour
{
    public static YusCoroutineManager Instance { get; private set; }

    private static bool _isQuitting;

    private int _nextId = 1;

    private sealed class TaskInfo
    {
        public int Id;
        public string Tag;
        public UnityEngine.Object Owner;
        public Coroutine Coroutine;
        public bool IsStopping;
        public float StartRealtime;
        public int StartFrame;
    }

    private readonly Dictionary<int, TaskInfo> _tasks = new Dictionary<int, TaskInfo>();

#if UNITY_EDITOR
    public readonly struct DebugTaskInfo
    {
        public int Id { get; }
        public string Tag { get; }
        public string OwnerName { get; }
        public string OwnerType { get; }
        public bool OwnerDestroyed { get; }
        public float AgeSeconds { get; }
        public int StartFrame { get; }

        public DebugTaskInfo(
            int id,
            string tag,
            string ownerName,
            string ownerType,
            bool ownerDestroyed,
            float ageSeconds,
            int startFrame)
        {
            Id = id;
            Tag = tag;
            OwnerName = ownerName;
            OwnerType = ownerType;
            OwnerDestroyed = ownerDestroyed;
            AgeSeconds = ageSeconds;
            StartFrame = startFrame;
        }
    }

    public List<DebugTaskInfo> DebugGetTasksSnapshot()
    {
        var list = new List<DebugTaskInfo>(_tasks.Count);
        float now = Time.realtimeSinceStartup;

        foreach (var kv in _tasks)
        {
            var t = kv.Value;

            bool ownerWasAssigned = ((object)t.Owner) != null;
            bool ownerDestroyed = ownerWasAssigned && t.Owner == null;
            string ownerName = ownerWasAssigned && !ownerDestroyed ? t.Owner.name : (ownerWasAssigned ? "<Destroyed>" : "<None>");
            string ownerType = ownerWasAssigned && !ownerDestroyed ? t.Owner.GetType().Name : (ownerWasAssigned ? "<Destroyed>" : "<None>");

            list.Add(new DebugTaskInfo(
                t.Id,
                t.Tag,
                ownerName,
                ownerType,
                ownerDestroyed,
                Mathf.Max(0f, now - t.StartRealtime),
                t.StartFrame));
        }

        return list;
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TryAttachToSingletonManagerOrPersist();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        _tasks.Clear();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void TryAttachToSingletonManagerOrPersist()
    {
        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance : FindObjectOfType<YusSingletonManager>();
        if (mgr != null)
        {
            if (transform.parent != mgr.transform)
            {
                transform.SetParent(mgr.transform, false);
            }

            if (mgr.Coroutine == null) mgr.Coroutine = this;
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private static bool EnsureInstance()
    {
        if (_isQuitting) return false;
        if (Instance != null) return true;

        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance : FindObjectOfType<YusSingletonManager>();
        if (mgr != null && mgr.Coroutine != null)
        {
            Instance = mgr.Coroutine;
            return Instance != null;
        }

        var found = FindObjectOfType<YusCoroutineManager>();
        if (found != null)
        {
            Instance = found;
            return true;
        }

        var go = new GameObject(nameof(YusCoroutineManager));
        Instance = go.AddComponent<YusCoroutineManager>();
        return Instance != null;
    }

    public static YusCoroutineHandle Run(IEnumerator routine, UnityEngine.Object owner = null, string tag = null)
    {
        if (routine == null) return default;
        if (!EnsureInstance() || Instance == null) return default;
        return Instance.StartInternal(routine, owner, tag);
    }

    public static YusCoroutineHandle Delay(
        float seconds,
        Action action,
        UnityEngine.Object owner = null,
        bool unscaledTime = false,
        string tag = null)
    {
        return Run(DelayRoutine(seconds, action, unscaledTime), owner, tag);
    }

    public static YusCoroutineHandle NextFrame(Action action, UnityEngine.Object owner = null, string tag = null)
    {
        return Run(NextFrameRoutine(action), owner, tag);
    }

    public static YusCoroutineHandle Repeat(
        float interval,
        Action action,
        int repeatCount = -1,
        float firstDelay = 0f,
        UnityEngine.Object owner = null,
        bool unscaledTime = false,
        string tag = null)
    {
        return Run(RepeatRoutine(interval, firstDelay, repeatCount, action, unscaledTime), owner, tag);
    }

    public static void Stop(int id)
    {
        if (id == 0) return;
        if (Instance == null) return;
        Instance.StopInternal(id);
    }

    public static bool IsRunning(int id)
    {
        if (id == 0) return false;
        return Instance != null && Instance._tasks.ContainsKey(id);
    }

    public static int StopTag(string tag)
    {
        if (Instance == null) return 0;
        return Instance.StopByTagInternal(tag);
    }

    public static int StopOwner(UnityEngine.Object owner)
    {
        if (Instance == null) return 0;
        return Instance.StopByOwnerInternal(owner);
    }

    public static void StopAll()
    {
        if (Instance == null) return;
        Instance.StopAllInternal();
    }

    private YusCoroutineHandle StartInternal(IEnumerator routine, UnityEngine.Object owner, string tag)
    {
        int id = _nextId++;
        var info = new TaskInfo
        {
            Id = id,
            Tag = tag,
            Owner = owner,
            StartRealtime = Time.realtimeSinceStartup,
            StartFrame = Time.frameCount,
        };

        _tasks[id] = info;
        info.Coroutine = StartCoroutine(GuardedRoutine(info, routine));
        return new YusCoroutineHandle(id);
    }

    private IEnumerator GuardedRoutine(TaskInfo info, IEnumerator routine)
    {
        while (true)
        {
            if (info.IsStopping) yield break;

            if (((object)info.Owner) != null && info.Owner == null)
            {
                RemoveIfMatches(info);
                yield break;
            }

            bool movedNext = false;
            object current = null;

            try
            {
                movedNext = routine.MoveNext();
                if (movedNext) current = routine.Current;
            }
            catch (Exception ex)
            {
                YusLogger.Error($"[YusCoroutine] Exception in coroutine (id={info.Id}, tag={info.Tag}): {ex}");
                RemoveIfMatches(info);
                yield break;
            }

            if (!movedNext)
            {
                RemoveIfMatches(info);
                yield break;
            }
            yield return current;
        }
    }

    private void StopInternal(int id)
    {
        if (!_tasks.TryGetValue(id, out var info)) return;
        if (info.IsStopping) return;

        info.IsStopping = true;
        if (info.Coroutine != null) StopCoroutine(info.Coroutine);
        _tasks.Remove(id);
    }

    private void RemoveIfMatches(TaskInfo info)
    {
        if (info == null) return;
        if (_tasks.TryGetValue(info.Id, out var existing) && ReferenceEquals(existing, info))
        {
            _tasks.Remove(info.Id);
        }
    }

    private int StopByTagInternal(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return 0;

        List<int> toStop = null;
        foreach (var kv in _tasks)
        {
            if (string.Equals(kv.Value.Tag, tag, StringComparison.Ordinal))
            {
                toStop ??= new List<int>();
                toStop.Add(kv.Key);
            }
        }

        if (toStop == null) return 0;
        foreach (int id in toStop) StopInternal(id);
        return toStop.Count;
    }

    private int StopByOwnerInternal(UnityEngine.Object owner)
    {
        if (((object)owner) == null) return 0;

        List<int> toStop = null;
        foreach (var kv in _tasks)
        {
            if (kv.Value.Owner == owner)
            {
                toStop ??= new List<int>();
                toStop.Add(kv.Key);
            }
        }

        if (toStop == null) return 0;
        foreach (int id in toStop) StopInternal(id);
        return toStop.Count;
    }

    private void StopAllInternal()
    {
        foreach (var kv in _tasks)
        {
            kv.Value.IsStopping = true;
            if (kv.Value.Coroutine != null) StopCoroutine(kv.Value.Coroutine);
        }
        _tasks.Clear();
    }

    private static IEnumerator DelayRoutine(float seconds, Action action, bool unscaledTime)
    {
        if (seconds > 0f)
        {
            yield return unscaledTime ? new WaitForSecondsRealtime(seconds) : new WaitForSeconds(seconds);
        }

        action?.Invoke();
    }

    private static IEnumerator NextFrameRoutine(Action action)
    {
        yield return null;
        action?.Invoke();
    }

    private static IEnumerator RepeatRoutine(
        float interval,
        float firstDelay,
        int repeatCount,
        Action action,
        bool unscaledTime)
    {
        if (firstDelay > 0f)
        {
            yield return unscaledTime ? new WaitForSecondsRealtime(firstDelay) : new WaitForSeconds(firstDelay);
        }

        int remaining = repeatCount;
        while (repeatCount < 0 || remaining-- > 0)
        {
            action?.Invoke();

            if (interval <= 0f)
            {
                yield return null;
            }
            else
            {
                yield return unscaledTime ? new WaitForSecondsRealtime(interval) : new WaitForSeconds(interval);
            }
        }
    }
}
