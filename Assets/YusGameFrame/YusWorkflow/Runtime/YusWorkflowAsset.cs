using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "YusGameFrame/Workflow/Workflow Asset", fileName = "YusWorkflow")]
public sealed class YusWorkflowAsset : ScriptableObject
{
    [Serializable]
    public sealed class NodeRecord
    {
        public string Guid;
        public Vector2 Position;

        [SerializeReference] public YusWorkflowNode Node;
    }

    [Serializable]
    public sealed class EdgeRecord
    {
        public string Guid;
        public string FromNodeGuid;
        public string FromPortName;
        public string ToNodeGuid;

        [SerializeReference] public YusWorkflowCondition Condition;
    }

    [SerializeField] private string entryNodeGuid;
    [SerializeField] private List<NodeRecord> nodes = new List<NodeRecord>();
    [SerializeField] private List<EdgeRecord> edges = new List<EdgeRecord>();

    public string EntryNodeGuid => entryNodeGuid;
    public IReadOnlyList<NodeRecord> Nodes => nodes;
    public IReadOnlyList<EdgeRecord> Edges => edges;

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (nodes == null || nodes.Count == 0)
        {
            errors.Add("[YusWorkflow] Asset has no nodes.");
            return false;
        }

        var nodeGuidSet = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < nodes.Count; i++)
        {
            var record = nodes[i];
            if (record == null)
            {
                errors.Add($"[YusWorkflow] NodeRecord[{i}] is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(record.Guid))
            {
                errors.Add($"[YusWorkflow] NodeRecord[{i}] Guid is empty.");
                continue;
            }

            if (!nodeGuidSet.Add(record.Guid))
            {
                errors.Add($"[YusWorkflow] Duplicate node Guid: {record.Guid}");
            }

            if (record.Node == null)
            {
                errors.Add($"[YusWorkflow] NodeRecord[{i}] Node is null. Guid={record.Guid}");
            }
        }

        if (!string.IsNullOrWhiteSpace(entryNodeGuid) && !nodeGuidSet.Contains(entryNodeGuid))
        {
            errors.Add($"[YusWorkflow] EntryNodeGuid not found in nodes: {entryNodeGuid}");
        }

        if (edges == null) return errors.Count == 0;

        var edgeGuidSet = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            if (edge == null)
            {
                errors.Add($"[YusWorkflow] EdgeRecord[{i}] is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(edge.Guid))
            {
                errors.Add($"[YusWorkflow] EdgeRecord[{i}] Guid is empty.");
            }
            else if (!edgeGuidSet.Add(edge.Guid))
            {
                errors.Add($"[YusWorkflow] Duplicate edge Guid: {edge.Guid}");
            }

            if (string.IsNullOrWhiteSpace(edge.FromNodeGuid))
            {
                errors.Add($"[YusWorkflow] EdgeRecord[{i}] FromNodeGuid is empty. EdgeGuid={edge.Guid}");
            }
            else if (!nodeGuidSet.Contains(edge.FromNodeGuid))
            {
                errors.Add($"[YusWorkflow] EdgeRecord[{i}] FromNodeGuid not found: {edge.FromNodeGuid}. EdgeGuid={edge.Guid}");
            }

            if (string.IsNullOrWhiteSpace(edge.ToNodeGuid))
            {
                errors.Add($"[YusWorkflow] EdgeRecord[{i}] ToNodeGuid is empty. EdgeGuid={edge.Guid}");
            }
            else if (!nodeGuidSet.Contains(edge.ToNodeGuid))
            {
                errors.Add($"[YusWorkflow] EdgeRecord[{i}] ToNodeGuid not found: {edge.ToNodeGuid}. EdgeGuid={edge.Guid}");
            }

            if (edge.FromPortName == null)
            {
                errors.Add($"[YusWorkflow] EdgeRecord[{i}] FromPortName is null. EdgeGuid={edge.Guid}");
            }
        }

        return errors.Count == 0;
    }

    public void Editor_SetEntry(string guid) => entryNodeGuid = guid;

    public void Editor_AddNode(NodeRecord record)
    {
        if (record == null) return;
        nodes.Add(record);
    }

    public void Editor_RemoveNodeGuid(string guid)
    {
        nodes.RemoveAll(n => n != null && n.Guid == guid);
        edges.RemoveAll(e => e != null && (e.FromNodeGuid == guid || e.ToNodeGuid == guid));
        if (entryNodeGuid == guid) entryNodeGuid = null;
    }

    public void Editor_AddEdge(EdgeRecord record)
    {
        if (record == null) return;
        edges.Add(record);
    }

    public void Editor_RemoveEdgeGuid(string guid)
    {
        edges.RemoveAll(e => e != null && e.Guid == guid);
    }
}
