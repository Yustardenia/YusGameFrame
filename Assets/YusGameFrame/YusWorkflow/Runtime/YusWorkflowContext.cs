using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class YusWorkflowContext
{
    public Component Owner { get; }
    public YusWorkflowAsset Asset { get; }

    public string CurrentNodeGuid { get; internal set; }

    public float DeltaTime { get; internal set; }
    public float FixedDeltaTime { get; internal set; }

    private readonly Dictionary<string, object> blackboard = new Dictionary<string, object>();
    private readonly Dictionary<string, object> initialBlackboard = new Dictionary<string, object>(StringComparer.Ordinal);

    public YusWorkflowEventBus Events { get; }

    private string requestedNodeGuid;

    public YusWorkflowContext(Component owner, YusWorkflowAsset asset)
    {
        Owner = owner;
        Asset = asset;
        Events = new YusWorkflowEventBus();
    }

    public T Get<T>(string key, T defaultValue = default)
    {
        if (!blackboard.TryGetValue(key, out var value)) return defaultValue;
        if (value is T t) return t;
        return defaultValue;
    }

    public void Set(string key, object value)
    {
        blackboard[key] = value;
    }

    internal void SetInitialBlackboardSnapshot(IEnumerable<YusWorkflowBlackboardEntry> entries)
    {
        initialBlackboard.Clear();
        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (entry == null) continue;
            if (string.IsNullOrWhiteSpace(entry.Key)) continue;

            object value;
            switch (entry.Kind)
            {
                case YusWorkflowBlackboardEntry.ValueKind.Int:
                    value = entry.IntValue;
                    break;
                case YusWorkflowBlackboardEntry.ValueKind.Float:
                    value = entry.FloatValue;
                    break;
                case YusWorkflowBlackboardEntry.ValueKind.Bool:
                    value = entry.BoolValue;
                    break;
                case YusWorkflowBlackboardEntry.ValueKind.String:
                    value = entry.StringValue;
                    break;
                default:
                    continue;
            }

            initialBlackboard[entry.Key] = value;
        }
    }

    internal void ApplyEntries(IEnumerable<YusWorkflowBlackboardEntry> entries)
    {
        if (entries == null) return;
        foreach (var entry in entries)
        {
            entry?.TryApplyTo(this);
        }
    }

    public void ResetBlackboardToInitial()
    {
        // Reset should also clean up event subscriptions stored outside the blackboard.
        Events.DisposeAll();
        requestedNodeGuid = null;

        blackboard.Clear();
        foreach (var kv in initialBlackboard)
        {
            blackboard[kv.Key] = kv.Value;
        }
    }

    public void RequestEnterNode(string nodeGuid)
    {
        requestedNodeGuid = nodeGuid;
    }

    internal string ConsumeRequestedNodeGuid()
    {
        var v = requestedNodeGuid;
        requestedNodeGuid = null;
        return v;
    }

    public void ResetEvent(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;
        Set(GetEventCountKey(eventName), 0);
    }

    public int GetEventCount(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return 0;
        return Get(GetEventCountKey(eventName), 0);
    }

    internal void MarkEventFired(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;
        var key = GetEventCountKey(eventName);
        var count = Get(key, 0);
        Set(key, count + 1);
    }

    internal static string GetEventCountKey(string eventName) => $"evt:{eventName}:count";

    internal static string GetEventSubscriptionKey(string nodeGuid, string eventName) =>
        $"evtSub:{nodeGuid}:{eventName}";

    internal void Dispose()
    {
        Events.DisposeAll();
        requestedNodeGuid = null;
        blackboard.Clear();
    }
}

public sealed class YusWorkflowEventBus
{
    private readonly List<IDisposable> subscriptions = new List<IDisposable>();

    public bool IsAvailable => YusEventManager.Instance != null;

    public IDisposable Subscribe(string eventName, Action handler)
    {
        if (string.IsNullOrWhiteSpace(eventName) || handler == null) return NoopDisposable.Instance;

        var manager = YusEventManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[YusWorkflow] YusEventManager not found, cannot subscribe: {eventName}");
            return NoopDisposable.Instance;
        }

        manager.AddListener(eventName, handler);
        var sub = new Subscription(this, eventName, handler);
        subscriptions.Add(sub);
        return sub;
    }

    public void Publish(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;
        var manager = YusEventManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[YusWorkflow] YusEventManager not found, cannot broadcast: {eventName}");
            return;
        }
        manager.Broadcast(eventName);
    }

    public void DisposeAll()
    {
        while (subscriptions.Count > 0)
        {
            try
            {
                subscriptions[subscriptions.Count - 1]?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[YusWorkflow] Dispose event subscription failed: {e.Message}");
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly YusWorkflowEventBus bus;
        private readonly string eventName;
        private readonly Action handler;
        private bool disposed;

        public Subscription(YusWorkflowEventBus bus, string eventName, Action handler)
        {
            this.bus = bus;
            this.eventName = eventName;
            this.handler = handler;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            var manager = YusEventManager.Instance;
            if (manager == null) return;
            manager.RemoveListener(eventName, handler);

            bus.subscriptions.Remove(this);
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new NoopDisposable();
        public void Dispose() { }
    }
}
