using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class YusWorkflowMachine
{
    public enum TransitionMode
    {
        Single,
        ResolveUntilStable
    }

    public enum TransitionReason
    {
        Start,
        Manual,
        Requested,
        AutoEdge,
        ExternalEvent
    }

    public readonly struct TransitionRecord
    {
        public readonly float Time;
        public readonly string FromGuid;
        public readonly string ToGuid;
        public readonly TransitionReason Reason;
        public readonly string EdgeGuid;
        public readonly string FromPortName;

        public TransitionRecord(float time, string fromGuid, string toGuid, TransitionReason reason, string edgeGuid, string fromPortName)
        {
            Time = time;
            FromGuid = fromGuid;
            ToGuid = toGuid;
            Reason = reason;
            EdgeGuid = edgeGuid;
            FromPortName = fromPortName;
        }
    }

    private readonly YusWorkflowContext context;
    private readonly YusWorkflowAsset asset;

    private readonly Dictionary<string, YusWorkflowAsset.NodeRecord> nodeByGuid =
        new Dictionary<string, YusWorkflowAsset.NodeRecord>();

    private readonly Dictionary<string, List<YusWorkflowAsset.EdgeRecord>> outgoingByGuid =
        new Dictionary<string, List<YusWorkflowAsset.EdgeRecord>>();

    public string CurrentNodeGuid { get; private set; }
    public string PreviousNodeGuid { get; private set; }

    public TransitionMode AutoTransitionMode { get; set; } = TransitionMode.Single;
    public int MaxTransitionsPerTick { get; set; } = 32;

    public event Action<TransitionRecord> OnTransition;

    private readonly List<TransitionRecord> transitionHistory = new List<TransitionRecord>(64);
    public IReadOnlyList<TransitionRecord> TransitionHistory => transitionHistory;

    public YusWorkflowMachine(Component owner, YusWorkflowAsset asset)
    {
        this.asset = asset;
        context = new YusWorkflowContext(owner, asset);
        BuildLookup();
    }

    public void Start()
    {
        if (asset == null)
        {
            Debug.LogError("[YusWorkflow] Asset is null.");
            return;
        }

        if (!asset.Validate(out var errors))
        {
            for (var i = 0; i < errors.Count; i++)
            {
                Debug.LogError(errors[i]);
            }
            return;
        }

        var entry = string.IsNullOrEmpty(asset.EntryNodeGuid) ? asset.Nodes[0].Guid : asset.EntryNodeGuid;
        EnterNode(entry, TransitionReason.Start, edgeGuid: null, fromPortName: null);
    }

    public void Tick()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (asset == null) return;
        if (string.IsNullOrEmpty(CurrentNodeGuid)) return;

        context.DeltaTime = deltaTime;

        FlushRequestedTransition();

        if (!nodeByGuid.TryGetValue(CurrentNodeGuid, out var nodeRecord)) return;
        context.CurrentNodeGuid = CurrentNodeGuid;
        nodeRecord.Node?.OnUpdate(context);

        ResolveAutoTransition();
    }

    public void FixedTick()
    {
        FixedTick(Time.fixedDeltaTime);
    }

    public void FixedTick(float fixedDeltaTime)
    {
        if (asset == null) return;
        if (string.IsNullOrEmpty(CurrentNodeGuid)) return;

        context.FixedDeltaTime = fixedDeltaTime;

        if (!nodeByGuid.TryGetValue(CurrentNodeGuid, out var nodeRecord)) return;
        context.CurrentNodeGuid = CurrentNodeGuid;
        nodeRecord.Node?.OnFixedUpdate(context);
    }

    public void Stop()
    {
        if (asset == null) return;
        ExitCurrent();
        CurrentNodeGuid = null;
        PreviousNodeGuid = null;
        context.Dispose();
    }

    public bool TryEnterNode(string nodeGuid)
    {
        return EnterNode(nodeGuid, TransitionReason.Manual, edgeGuid: null, fromPortName: null);
    }

    private void BuildLookup()
    {
        nodeByGuid.Clear();
        outgoingByGuid.Clear();

        if (asset == null) return;

        foreach (var node in asset.Nodes)
        {
            if (node == null) continue;
            if (string.IsNullOrEmpty(node.Guid)) continue;
            nodeByGuid[node.Guid] = node;
        }

        foreach (var edge in asset.Edges)
        {
            if (edge == null) continue;
            if (string.IsNullOrEmpty(edge.FromNodeGuid)) continue;
            if (!outgoingByGuid.TryGetValue(edge.FromNodeGuid, out var list))
            {
                list = new List<YusWorkflowAsset.EdgeRecord>();
                outgoingByGuid[edge.FromNodeGuid] = list;
            }
            list.Add(edge);
        }
    }

    internal bool EnterNodeFromExternalEvent(string nodeGuid)
    {
        return EnterNode(nodeGuid, TransitionReason.ExternalEvent, edgeGuid: null, fromPortName: null);
    }

    private bool EnterNode(string nodeGuid, TransitionReason reason, string edgeGuid, string fromPortName)
    {
        if (string.IsNullOrEmpty(nodeGuid)) return false;
        if (!nodeByGuid.TryGetValue(nodeGuid, out var nodeRecord)) return false;
        if (nodeRecord.Node == null) return false;

        if (CurrentNodeGuid == nodeGuid) return true;

        ExitCurrent();

        var from = CurrentNodeGuid;
        PreviousNodeGuid = CurrentNodeGuid;
        CurrentNodeGuid = nodeGuid;
        context.CurrentNodeGuid = CurrentNodeGuid;
        RecordTransition(from, CurrentNodeGuid, reason, edgeGuid, fromPortName);
        nodeRecord.Node.OnEnter(context);
        return true;
    }

    private void ExitCurrent()
    {
        if (string.IsNullOrEmpty(CurrentNodeGuid)) return;
        if (!nodeByGuid.TryGetValue(CurrentNodeGuid, out var nodeRecord)) return;
        context.CurrentNodeGuid = CurrentNodeGuid;
        nodeRecord.Node?.OnExit(context);
    }

    private void TryAutoTransition(YusWorkflowAsset.NodeRecord fromNode)
    {
        if (fromNode == null) return;
        if (!outgoingByGuid.TryGetValue(fromNode.Guid, out var edges) || edges == null) return;

        var portOrder = fromNode.Node?.GetOutputPortNames()?.ToList() ?? new List<string>();
        if (portOrder.Count == 0) portOrder.Add("Next");

        // Evaluate edges grouped by output port, in port order (ComfyUI-like explicit branching).
        for (var p = 0; p < portOrder.Count; p++)
        {
            var portName = portOrder[p];
            for (var i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];
                if (edge == null) continue;
                if (!string.Equals(edge.FromPortName, portName, StringComparison.Ordinal)) continue;

                var ok = edge.Condition == null || edge.Condition.Evaluate(context);
                if (!ok) continue;

                if (EnterNode(edge.ToNodeGuid, TransitionReason.AutoEdge, edge.Guid, portName)) return;
            }
        }

        // Fallback: evaluate edges whose port name isn't declared by the node.
        for (var i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            if (edge == null) continue;
            if (portOrder.Contains(edge.FromPortName)) continue;

            var ok = edge.Condition == null || edge.Condition.Evaluate(context);
            if (!ok) continue;

            if (EnterNode(edge.ToNodeGuid, TransitionReason.AutoEdge, edge.Guid, edge.FromPortName)) return;
        }
    }

    private void FlushRequestedTransition()
    {
        var requested = context.ConsumeRequestedNodeGuid();
        if (string.IsNullOrEmpty(requested)) return;
        EnterNode(requested, TransitionReason.Requested, edgeGuid: null, fromPortName: null);
    }

    private void ResolveAutoTransition()
    {
        if (AutoTransitionMode == TransitionMode.Single)
        {
            if (!nodeByGuid.TryGetValue(CurrentNodeGuid, out var fromNode)) return;
            TryAutoTransition(fromNode);
            return;
        }

        var remaining = Mathf.Max(1, MaxTransitionsPerTick);
        while (remaining-- > 0)
        {
            if (string.IsNullOrEmpty(CurrentNodeGuid)) return;
            if (!nodeByGuid.TryGetValue(CurrentNodeGuid, out var fromNode)) return;

            var before = CurrentNodeGuid;
            TryAutoTransition(fromNode);
            if (CurrentNodeGuid == before) return;

            FlushRequestedTransition();
        }

        Debug.LogWarning($"[YusWorkflow] Auto-transition exceeded MaxTransitionsPerTick={MaxTransitionsPerTick}, stopped to avoid infinite loop. Current={CurrentNodeGuid}");
    }

    private void RecordTransition(string fromGuid, string toGuid, TransitionReason reason, string edgeGuid, string fromPortName)
    {
        var record = new TransitionRecord(Time.unscaledTime, fromGuid, toGuid, reason, edgeGuid, fromPortName);
        if (transitionHistory.Count >= 64) transitionHistory.RemoveAt(0);
        transitionHistory.Add(record);
        OnTransition?.Invoke(record);
    }
}
