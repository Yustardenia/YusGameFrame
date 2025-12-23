using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class YusWorkflowNodeView : Node
{
    public string Guid { get; }
    public Type NodeType => record?.Node?.GetType();

    public Port Input { get; private set; }

    public Action<string> OnRequestSetEntry;
    public Action<Vector2> OnNodeMoved;

    private YusWorkflowAsset.NodeRecord record;
    private readonly Label entryBadge;
    private readonly Label runtimeBadge;
    private readonly Dictionary<string, Port> outputsByName = new Dictionary<string, Port>();
    private Color? defaultBorderColor;
    private float? defaultBorderWidth;

    public YusWorkflowNodeView(YusWorkflowAsset.NodeRecord record)
    {
        Guid = record.Guid;
        BindRecord(record);

        entryBadge = new Label("ENTRY");
        entryBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        entryBadge.style.color = Color.yellow;
        entryBadge.style.display = DisplayStyle.None;
        titleContainer.Add(entryBadge);

        runtimeBadge = new Label("RUN");
        runtimeBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        runtimeBadge.style.color = new Color(0.2f, 1f, 0.6f);
        runtimeBadge.style.display = DisplayStyle.None;
        titleContainer.Add(runtimeBadge);

        extensionContainer.Add(new Button(() => OnRequestSetEntry?.Invoke(Guid)) { text = "Set Entry" });

        RefreshExpandedState();
        RefreshPorts();
    }

    public void BindRecord(YusWorkflowAsset.NodeRecord record)
    {
        this.record = record;
        title = record?.Node == null ? "Node" : record.Node.GetTitle();
        RebuildPorts();
    }

    public void RebuildPorts()
    {
        inputContainer.Clear();
        outputContainer.Clear();
        outputsByName.Clear();

        Input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        Input.portName = "In";
        inputContainer.Add(Input);

        var outputNames = record?.Node?.GetOutputPortNames()?.ToList() ?? new List<string> { "Next" };
        if (outputNames.Count == 0) outputNames.Add("Next");

        for (var i = 0; i < outputNames.Count; i++)
        {
            var name = string.IsNullOrEmpty(outputNames[i]) ? $"Out{i}" : outputNames[i];
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = name;
            outputContainer.Add(port);
            outputsByName[name] = port;
        }

        RefreshPorts();
        RefreshExpandedState();
    }

    public Port GetOrCreateOutput(string portName)
    {
        portName = string.IsNullOrEmpty(portName) ? "Next" : portName;
        if (outputsByName.TryGetValue(portName, out var existing)) return existing;

        var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        port.portName = portName;
        outputContainer.Add(port);
        outputsByName[portName] = port;
        RefreshPorts();
        RefreshExpandedState();
        return port;
    }

    public void SetIsEntry(bool isEntry)
    {
        entryBadge.style.display = isEntry ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetIsRuntimeCurrent(bool isCurrent)
    {
        runtimeBadge.style.display = isCurrent ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetRuntimeVisual(Color? borderColor, float borderWidth, float opacity)
    {
        if (defaultBorderColor == null)
        {
            defaultBorderColor = mainContainer.style.borderTopColor.value;
        }

        if (defaultBorderWidth == null)
        {
            defaultBorderWidth = mainContainer.style.borderTopWidth.value;
        }

        if (borderColor == null || opacity <= 0f)
        {
            var c = defaultBorderColor ?? new Color(0.2f, 0.2f, 0.2f);
            var w = defaultBorderWidth ?? 1f;
            mainContainer.style.borderLeftColor = c;
            mainContainer.style.borderRightColor = c;
            mainContainer.style.borderTopColor = c;
            mainContainer.style.borderBottomColor = c;
            mainContainer.style.borderLeftWidth = w;
            mainContainer.style.borderRightWidth = w;
            mainContainer.style.borderTopWidth = w;
            mainContainer.style.borderBottomWidth = w;
            return;
        }

        var color = borderColor.Value;
        color.a = Mathf.Clamp01(opacity);
        mainContainer.style.borderLeftColor = color;
        mainContainer.style.borderRightColor = color;
        mainContainer.style.borderTopColor = color;
        mainContainer.style.borderBottomColor = color;

        var width = Mathf.Max(1f, borderWidth);
        mainContainer.style.borderLeftWidth = width;
        mainContainer.style.borderRightWidth = width;
        mainContainer.style.borderTopWidth = width;
        mainContainer.style.borderBottomWidth = width;
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        OnNodeMoved?.Invoke(newPos.position);
    }
}
