using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class YusWorkflowGraphView : GraphView
{
    public Action<ISelectable> OnSelectionChanged;

    private YusWorkflowAsset asset;
    private readonly Dictionary<string, YusWorkflowNodeView> nodeViewsByGuid = new Dictionary<string, YusWorkflowNodeView>();
    private readonly Dictionary<string, Edge> edgeViewsByGuid = new Dictionary<string, Edge>();
    private YusWorkflowSearchWindowProvider searchProvider;
    private string runtimeCurrentGuid;
    private string runtimeEdgeGuid;
    private float runtimeEdgeTime;
    private readonly Color runtimeEdgeColor = new Color(0.2f, 1f, 0.6f);
    private readonly Color defaultEdgeColor = new Color(0.7f, 0.7f, 0.7f);
    private const float RuntimeEdgeFlashSeconds = 0.25f;

    private string runtimeFromGuid;
    private string runtimeToGuid;
    private float runtimeNodeFlashTime;
    private const float RuntimeNodeFlashSeconds = 0.25f;
    private const float RuntimeTrailSeconds = 2.5f;
    private const int RuntimeTrailMax = 8;
    private readonly List<(string guid, float time)> runtimeTrail = new List<(string, float)>(RuntimeTrailMax);
    private readonly Color currentNodeColor = new Color(0.2f, 1f, 0.6f);
    private readonly Color transitionToColor = new Color(1f, 0.85f, 0.2f);
    private readonly Color transitionFromColor = new Color(0.2f, 0.8f, 1f);

    public YusWorkflowGraphView()
    {
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        graphViewChanged = OnGraphViewChanged;
    }

    public void AttachSearch(EditorWindow window)
    {
        if (searchProvider == null)
        {
            searchProvider = ScriptableObject.CreateInstance<YusWorkflowSearchWindowProvider>();
        }
        searchProvider.Init(this, window);

        nodeCreationRequest = ctx =>
        {
            SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), searchProvider);
        };
    }

    public Vector2 ScreenToGraphPosition(Vector2 screenPosition, EditorWindow window)
    {
        var worldPosition = screenPosition;
        if (window != null)
        {
            worldPosition -= window.position.position;
        }
        return contentViewContainer.WorldToLocal(worldPosition);
    }

    public override void AddToSelection(ISelectable selectable)
    {
        base.AddToSelection(selectable);
        OnSelectionChanged?.Invoke(selectable);
    }

    public override void ClearSelection()
    {
        base.ClearSelection();
        OnSelectionChanged?.Invoke(null);
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        evt.menu.AppendAction("创建节点/空节点", _ =>
        {
            if (asset == null) return;
            var pos = contentViewContainer.WorldToLocal(evt.mousePosition);
            CreateNode(pos, typeof(YusWorkflowEmptyNode));
        });

        var nodeTypes = YusWorkflowEditorTypeUtil.GetNodeTypes();
        for (var i = 0; i < nodeTypes.Count; i++)
        {
            var type = nodeTypes[i];
            if (type == null) continue;
            if (type == typeof(YusWorkflowEmptyNode)) continue;

            var label = YusWorkflowEditorTypeUtil.GetNodeDisplayName(type);
            evt.menu.AppendAction($"创建节点/{label}", _ =>
            {
                if (asset == null) return;
                var pos = contentViewContainer.WorldToLocal(evt.mousePosition);
                CreateNode(pos, type);
            });
        }
    }

    public void Populate(YusWorkflowAsset asset)
    {
        this.asset = asset;
        DeleteElements(graphElements.ToList());
        nodeViewsByGuid.Clear();
        edgeViewsByGuid.Clear();
        runtimeCurrentGuid = null;
        runtimeEdgeGuid = null;
        runtimeEdgeTime = 0f;
        runtimeFromGuid = null;
        runtimeToGuid = null;
        runtimeNodeFlashTime = 0f;
        runtimeTrail.Clear();

        Insert(0, new GridBackground());

        if (asset == null) return;

        foreach (var nodeRecord in asset.Nodes)
        {
            if (nodeRecord == null) continue;
            var view = CreateNodeView(nodeRecord);
            AddElement(view);
        }

        foreach (var edgeRecord in asset.Edges)
        {
            if (edgeRecord == null) continue;
            if (!nodeViewsByGuid.TryGetValue(edgeRecord.FromNodeGuid, out var fromView)) continue;
            if (!nodeViewsByGuid.TryGetValue(edgeRecord.ToNodeGuid, out var toView)) continue;

            var outputPort = fromView.GetOrCreateOutput(edgeRecord.FromPortName);
            if (outputPort == null) continue;

            var edge = outputPort.ConnectTo(toView.Input);
            edge.userData = new YusWorkflowEdgeUserData { EdgeGuid = edgeRecord.Guid };
            if (!string.IsNullOrEmpty(edgeRecord.Guid))
            {
                edgeViewsByGuid[edgeRecord.Guid] = edge;
            }
            AddElement(edge);
        }

        MarkEntryBadge();
        MarkRuntimeBadge();
        UpdateRuntimeVisuals();
    }

    public void CreateNodeAtCenter()
    {
        if (asset == null) return;
        var pos = contentViewContainer.WorldToLocal(worldBound.center);
        CreateNode(pos, typeof(YusWorkflowEmptyNode));
    }

    public void OpenSearchAt(Vector2 screenMousePosition)
    {
        if (searchProvider == null) return;
        SearchWindow.Open(new SearchWindowContext(screenMousePosition), searchProvider);
    }

    public void CreateNodeAt(Vector2 graphPosition, Type nodeType)
    {
        CreateNode(graphPosition, nodeType);
    }

    public void SetEntry(string guid)
    {
        if (asset == null) return;
        asset.Editor_SetEntry(guid);
        EditorUtility.SetDirty(asset);
        MarkEntryBadge();
    }

    public void SetRuntimeCurrentNode(string guid)
    {
        runtimeCurrentGuid = guid;
        MarkRuntimeBadge();
    }

    public void NotifyRuntimeTransition(string fromGuid, string toGuid, string edgeGuid, float time)
    {
        runtimeFromGuid = fromGuid;
        runtimeToGuid = toGuid;
        runtimeNodeFlashTime = time;

        runtimeEdgeGuid = edgeGuid;
        runtimeEdgeTime = time;
        if (!string.IsNullOrEmpty(toGuid))
        {
            runtimeTrail.Add((toGuid, time));
            if (runtimeTrail.Count > RuntimeTrailMax) runtimeTrail.RemoveAt(0);
        }
        UpdateRuntimeVisuals();
    }

    public void ClearRuntimeTransition()
    {
        runtimeFromGuid = null;
        runtimeToGuid = null;
        runtimeNodeFlashTime = 0f;
        runtimeEdgeGuid = null;
        runtimeEdgeTime = 0f;
        runtimeTrail.Clear();
        UpdateRuntimeVisuals();
    }

    public void UpdateRuntimeVisuals()
    {
        var now = Time.unscaledTime;

        var intensity = 0f;
        if (!string.IsNullOrEmpty(runtimeEdgeGuid))
        {
            intensity = Mathf.Clamp01(1f - (now - runtimeEdgeTime) / RuntimeEdgeFlashSeconds);
            if (intensity <= 0f)
            {
                runtimeEdgeGuid = null;
                runtimeEdgeTime = 0f;
            }
        }

        var nodeFlash = 0f;
        if (!string.IsNullOrEmpty(runtimeToGuid) || !string.IsNullOrEmpty(runtimeFromGuid))
        {
            nodeFlash = Mathf.Clamp01(1f - (now - runtimeNodeFlashTime) / RuntimeNodeFlashSeconds);
            if (nodeFlash <= 0f)
            {
                runtimeFromGuid = null;
                runtimeToGuid = null;
                runtimeNodeFlashTime = 0f;
            }
        }

        // Drop expired trail items.
        for (var i = runtimeTrail.Count - 1; i >= 0; i--)
        {
            if (now - runtimeTrail[i].time > RuntimeTrailSeconds)
            {
                runtimeTrail.RemoveAt(i);
            }
        }

        foreach (var kv in edgeViewsByGuid)
        {
            var isActive = !string.IsNullOrEmpty(runtimeEdgeGuid) && string.Equals(kv.Key, runtimeEdgeGuid, StringComparison.Ordinal);
            ApplyEdgeStyle(kv.Value, isActive ? intensity : 0f);
        }

        foreach (var view in nodeViewsByGuid.Values)
        {
            if (view == null) continue;

            var guid = view.Guid;
            var isCurrent = !string.IsNullOrEmpty(runtimeCurrentGuid) && string.Equals(guid, runtimeCurrentGuid, StringComparison.Ordinal);
            var isTo = !string.IsNullOrEmpty(runtimeToGuid) && string.Equals(guid, runtimeToGuid, StringComparison.Ordinal);
            var isFrom = !string.IsNullOrEmpty(runtimeFromGuid) && string.Equals(guid, runtimeFromGuid, StringComparison.Ordinal);

            var trailIntensity = 0f;
            for (var i = 0; i < runtimeTrail.Count; i++)
            {
                if (!string.Equals(runtimeTrail[i].guid, guid, StringComparison.Ordinal)) continue;
                var t = Mathf.Clamp01(1f - (now - runtimeTrail[i].time) / RuntimeTrailSeconds);
                trailIntensity = Mathf.Max(trailIntensity, t * 0.5f);
            }

            if (isCurrent)
            {
                view.SetRuntimeVisual(currentNodeColor, 4f, 1f);
                continue;
            }

            if (isTo && nodeFlash > 0f)
            {
                view.SetRuntimeVisual(transitionToColor, 4f, nodeFlash);
                continue;
            }

            if (isFrom && nodeFlash > 0f)
            {
                view.SetRuntimeVisual(transitionFromColor, 4f, nodeFlash);
                continue;
            }

            if (trailIntensity > 0f)
            {
                view.SetRuntimeVisual(currentNodeColor, 3f, trailIntensity);
                continue;
            }

            view.SetRuntimeVisual(null, 0f, 0f);
        }
    }

    public void ChangeNodeType(string guid, Type nodeType)
    {
        if (asset == null) return;
        if (nodeType == null) return;

        var record = asset.Nodes.FirstOrDefault(n => n != null && n.Guid == guid);
        if (record == null) return;

        record.Node = Activator.CreateInstance(nodeType) as YusWorkflowNode;
        if (record.Node == null) return;

        EditorUtility.SetDirty(asset);

        if (nodeViewsByGuid.TryGetValue(guid, out var view))
        {
            view.BindRecord(record);
            RefreshNodePorts(guid);
        }
    }

    public void RefreshNodePorts(string guid)
    {
        if (!nodeViewsByGuid.TryGetValue(guid, out var view)) return;

        var existingEdges = edges.ToList()
            .Where(e => e?.output?.node == view)
            .ToList();

        foreach (var edge in existingEdges)
        {
            if (edge?.userData is not YusWorkflowEdgeUserData ud) continue;
            asset?.Editor_RemoveEdgeGuid(ud.EdgeGuid);
            RemoveElement(edge);
        }

        view.RebuildPorts();

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    public void RefreshNodeTitle(string guid)
    {
        if (!nodeViewsByGuid.TryGetValue(guid, out var view)) return;
        var record = asset?.Nodes.FirstOrDefault(n => n != null && n.Guid == guid);
        if (record == null) return;
        view.BindRecord(record);
    }

    public void SavePositionsToAsset()
    {
        if (asset == null) return;
        foreach (var view in nodes.OfType<YusWorkflowNodeView>())
        {
            var record = asset.Nodes.FirstOrDefault(n => n != null && n.Guid == view.Guid);
            if (record == null) continue;
            record.Position = view.GetPosition().position;
        }
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private void CreateNode(Vector2 position, Type nodeType)
    {
        if (asset == null) return;
        if (nodeType == null) return;

        var guid = Guid.NewGuid().ToString("N");
        var node = Activator.CreateInstance(nodeType) as YusWorkflowNode;
        if (node == null) return;

        var record = new YusWorkflowAsset.NodeRecord
        {
            Guid = guid,
            Position = position,
            Node = node
        };

        asset.Editor_AddNode(record);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        var view = CreateNodeView(record);
        AddElement(view);
        MarkEntryBadge();
    }

    private YusWorkflowNodeView CreateNodeView(YusWorkflowAsset.NodeRecord record)
    {
        var view = new YusWorkflowNodeView(record);
        view.OnRequestSetEntry = SetEntry;
        view.OnNodeMoved = pos =>
        {
            record.Position = pos;
            EditorUtility.SetDirty(asset);
        };
        nodeViewsByGuid[record.Guid] = view;
        view.SetPosition(new Rect(record.Position, new Vector2(280, 180)));
        return view;
    }

    private void MarkEntryBadge()
    {
        if (asset == null) return;
        foreach (var view in nodes.OfType<YusWorkflowNodeView>())
        {
            view.SetIsEntry(view.Guid == asset.EntryNodeGuid);
        }
    }

    private void MarkRuntimeBadge()
    {
        foreach (var view in nodes.OfType<YusWorkflowNodeView>())
        {
            view.SetIsRuntimeCurrent(!string.IsNullOrEmpty(runtimeCurrentGuid) && view.Guid == runtimeCurrentGuid);
        }
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (asset == null) return change;

        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                if (edge?.output?.node is not YusWorkflowNodeView fromView) continue;
                if (edge?.input?.node is not YusWorkflowNodeView toView) continue;

                var portName = edge.output.portName;
                if (string.IsNullOrEmpty(portName)) portName = "Next";

                var record = new YusWorkflowAsset.EdgeRecord
                {
                    Guid = Guid.NewGuid().ToString("N"),
                    FromNodeGuid = fromView.Guid,
                    FromPortName = portName,
                    ToNodeGuid = toView.Guid,
                    Condition = null
                };

                asset.Editor_AddEdge(record);
                edge.userData = new YusWorkflowEdgeUserData { EdgeGuid = record.Guid };
                edgeViewsByGuid[record.Guid] = edge;
                EditorUtility.SetDirty(asset);
            }
        }

        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove)
            {
                switch (element)
                {
                    case Edge edge:
                        if (edge.userData is YusWorkflowEdgeUserData ud)
                        {
                            asset.Editor_RemoveEdgeGuid(ud.EdgeGuid);
                            edgeViewsByGuid.Remove(ud.EdgeGuid);
                            EditorUtility.SetDirty(asset);
                        }
                        break;
                    case YusWorkflowNodeView nodeView:
                        asset.Editor_RemoveNodeGuid(nodeView.Guid);
                        nodeViewsByGuid.Remove(nodeView.Guid);
                        EditorUtility.SetDirty(asset);
                        break;
                }
            }
        }

        return change;
    }

    private void ApplyEdgeStyle(Edge edge, float intensity)
    {
        if (edge == null) return;
        var control = edge.edgeControl;
        if (control == null) return;

        var color = Color.Lerp(defaultEdgeColor, runtimeEdgeColor, intensity);
        control.inputColor = color;
        control.outputColor = color;
        control.edgeWidth = intensity > 0f ? 4 : 2;
        edge.MarkDirtyRepaint();
    }
}

internal sealed class YusWorkflowEdgeUserData
{
    public string EdgeGuid;
}
