using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class YusWorkflowEditorWindow : EditorWindow
{
    private YusWorkflowAsset asset;
    private YusWorkflowGraphView graphView;
    private VisualElement inspector;
    private List<string> lastValidationErrors = new List<string>();
    private VisualElement inspectorTop;
    private VisualElement inspectorContent;
    private HelpBox validationBox;
    private VisualElement runtimePanel;
    private Label runtimeStateLabel;
    private Label runtimeHistoryLabel;
    private HelpBox runtimeInfoBox;
    private PopupField<string> runtimeRunnerPopup;
    private Toggle runtimeManualToggle;
    private FloatField runtimeDeltaScaleField;
    private double lastRuntimeScanTime;

    private readonly List<YusWorkflowRunner> runtimeRunners = new List<YusWorkflowRunner>();
    private int runtimeRunnerIndex;
    private readonly Dictionary<int, int> lastHistoryCountByRunnerId = new Dictionary<int, int>();

    [MenuItem(YusGameFrameEditorMenu.Root + "Workflow/工作流编辑器")]
    public static void Open()
    {
        GetWindow<YusWorkflowEditorWindow>("Yus 工作流");
    }

    private void OnEnable()
    {
        if (rootVisualElement.childCount == 0)
        {
            BuildUI();
        }

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
    }

    private void BuildUI()
    {
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        var toolbar = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                paddingLeft = 6,
                paddingRight = 6,
                paddingTop = 4,
                paddingBottom = 4,
                alignItems = Align.Center,
                backgroundColor = new Color(0.18f, 0.18f, 0.18f)
            }
        };

        var assetField = new ObjectField("工作流资源")
        {
            objectType = typeof(YusWorkflowAsset),
            allowSceneObjects = false,
            value = asset
        };
        assetField.style.flexGrow = 1;
        assetField.RegisterValueChangedCallback(evt =>
        {
            asset = evt.newValue as YusWorkflowAsset;
            graphView?.Populate(asset);
            ValidateAsset();
            ShowNothingSelected();
        });
        toolbar.Add(assetField);

        toolbar.Add(new Button(CreateAsset) { text = "新建资源" });
        toolbar.Add(new Button(() =>
        {
            ValidateAsset();
            ShowNothingSelected();
        }) { text = "Validate" });
        toolbar.Add(new Button(() => graphView?.OpenSearchAt(new Vector2(position.x + position.width * 0.5f, position.y + position.height * 0.5f))) { text = "添加节点(搜索)" });
        toolbar.Add(new Button(() => graphView?.CreateNodeAtCenter()) { text = "添加空节点" });
        toolbar.Add(new Button(() => YusWorkflowNodeGeneratorWindow.Open()) { text = "生成节点脚本" });
        toolbar.Add(new Button(SaveAsset) { text = "保存" });

        rootVisualElement.Add(toolbar);

        var body = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };

        graphView = new YusWorkflowGraphView();
        graphView.style.flexGrow = 1;
        graphView.OnSelectionChanged = OnGraphSelectionChanged;
        body.Add(graphView);
        graphView.AttachSearch(this);

        inspector = new VisualElement
        {
            style =
            {
                width = 360,
                paddingLeft = 8,
                paddingRight = 8,
                paddingTop = 8,
                backgroundColor = new Color(0.12f, 0.12f, 0.12f)
            }
        };
        inspectorTop = new VisualElement();
        inspectorContent = new VisualElement();
        inspector.Add(inspectorTop);
        inspector.Add(inspectorContent);

        validationBox = new HelpBox(string.Empty, HelpBoxMessageType.Error);
        validationBox.style.display = DisplayStyle.None;
        inspectorTop.Add(validationBox);

        runtimePanel = new VisualElement();
        runtimePanel.style.marginTop = 8;
        inspectorTop.Add(runtimePanel);
        BuildRuntimePanelUI();

        body.Add(inspector);

        rootVisualElement.Add(body);

        graphView.Populate(asset);
        ShowNothingSelected();
    }

    private void CreateAsset()
    {
        var path = EditorUtility.SaveFilePanelInProject(
            "创建工作流资源",
            "YusWorkflow",
            "asset",
            "选择工作流资源保存位置");
        if (string.IsNullOrEmpty(path)) return;

        var created = CreateInstance<YusWorkflowAsset>();
        AssetDatabase.CreateAsset(created, path);
        AssetDatabase.SaveAssets();

        asset = created;
        graphView?.Populate(asset);
        ValidateAsset();
        ShowNothingSelected();
    }

    private void SaveAsset()
    {
        graphView?.SavePositionsToAsset();
        if (asset != null) YusWorkflowBlackboardKeysGenerator.GenerateFor(asset);
    }

    private void OnGraphSelectionChanged(ISelectable selectable)
    {
        inspectorContent.Clear();

        switch (selectable)
        {
            case YusWorkflowNodeView nodeView:
                DrawNodeInspector(nodeView);
                break;
            case Edge edge:
                DrawEdgeInspector(edge);
                break;
            default:
                ShowNothingSelected();
                break;
        }
    }

    private void ShowNothingSelected()
    {
        ValidateAsset();
        inspectorContent.Clear();
        inspectorContent.Add(new Label("选择一个节点/连线进行编辑。"));
        inspectorContent.Add(new Label("提示：按空格打开搜索，或使用“添加节点(搜索)”"));

        if (asset == null) return;

        inspectorContent.Add(new VisualElement { style = { height = 8 } });
        inspectorContent.Add(new Label("Context Init (Blackboard)"));

        var so = new SerializedObject(asset);
        var initProp = so.FindProperty("initialBlackboard");
        if (initProp == null)
        {
            inspectorContent.Add(new HelpBox("initialBlackboard 字段未找到，请确认 YusWorkflowAsset 已更新。", HelpBoxMessageType.Warning));
            return;
        }

        var field = new PropertyField(initProp);
        field.Bind(so);
        inspectorContent.Add(field);
    }

    private void ValidateAsset()
    {
        lastValidationErrors.Clear();
        if (asset == null) return;
        asset.Validate(out lastValidationErrors);
        UpdateValidationBox();
    }

    private void UpdateValidationBox()
    {
        if (validationBox == null) return;

        if (asset == null || lastValidationErrors == null || lastValidationErrors.Count == 0)
        {
            validationBox.text = string.Empty;
            validationBox.style.display = DisplayStyle.None;
            return;
        }

        var text = string.Join("\n", lastValidationErrors.Take(12));
        if (lastValidationErrors.Count > 12) text += $"\n... (+{lastValidationErrors.Count - 12})";
        validationBox.text = text;
        validationBox.style.display = DisplayStyle.Flex;
    }

    private void EditorUpdate()
    {
        if (graphView == null) return;

        if (!Application.isPlaying)
        {
            runtimeRunners.Clear();
            runtimeRunnerIndex = 0;
            lastHistoryCountByRunnerId.Clear();
            graphView.SetRuntimeCurrentNode(null);
            graphView.ClearRuntimeTransition();
            UpdateRuntimePanel(null);
            return;
        }

        if (asset == null)
        {
            runtimeRunners.Clear();
            runtimeRunnerIndex = 0;
            lastHistoryCountByRunnerId.Clear();
            graphView.SetRuntimeCurrentNode(null);
            graphView.ClearRuntimeTransition();
            UpdateRuntimePanel(null);
            return;
        }

        RefreshRuntimeRunnersIfNeeded();
        if (runtimeRunnerIndex < 0) runtimeRunnerIndex = 0;
        if (runtimeRunnerIndex >= runtimeRunners.Count) runtimeRunnerIndex = 0;

        var runner = runtimeRunners.Count > 0 ? runtimeRunners[runtimeRunnerIndex] : null;
        var machine = runner?.Machine;

        graphView.SetRuntimeCurrentNode(machine?.CurrentNodeGuid);
        NotifyGraphRuntimeTransition(runner, machine);
        UpdateRuntimePanel(machine);
    }

    private void NotifyGraphRuntimeTransition(YusWorkflowRunner runner, YusWorkflowMachine machine)
    {
        if (graphView == null) return;
        if (runner == null || machine == null)
        {
            graphView.ClearRuntimeTransition();
            return;
        }

        var runnerId = runner.GetInstanceID();
        var history = machine.TransitionHistory;
        var count = history?.Count ?? 0;
        if (!lastHistoryCountByRunnerId.TryGetValue(runnerId, out var lastCount))
        {
            lastCount = 0;
        }

        if (count > lastCount && count > 0)
        {
            var last = history[count - 1];
            graphView.NotifyRuntimeTransition(last.FromGuid, last.ToGuid, last.EdgeGuid, last.Time);
        }

        lastHistoryCountByRunnerId[runnerId] = count;
        graphView.UpdateRuntimeVisuals();
    }

    private void BuildRuntimePanelUI()
    {
        if (runtimePanel == null) return;

        runtimePanel.Add(new Label("PlayMode Debug"));

        runtimeInfoBox = new HelpBox("进入 PlayMode 后显示运行状态。", HelpBoxMessageType.Info);
        runtimePanel.Add(runtimeInfoBox);

        runtimeRunnerPopup = new PopupField<string>("Runner", new List<string> { "(none)" }, 0);
        runtimeRunnerPopup.RegisterValueChangedCallback(evt =>
        {
            if (runtimeRunnerPopup?.choices == null) return;
            var idx = runtimeRunnerPopup.choices.IndexOf(evt.newValue);
            if (idx >= 0) runtimeRunnerIndex = idx;
        });
        runtimePanel.Add(runtimeRunnerPopup);

        var controlRow = new VisualElement();
        controlRow.style.flexDirection = FlexDirection.Row;
        controlRow.style.marginBottom = 4;
        runtimePanel.Add(controlRow);

        var pauseBtn = new Button(() =>
        {
            var runner = GetSelectedRuntimeRunner();
            if (runner == null) return;
            runner.DebugPaused = !runner.DebugPaused;
        })
        { text = "Pause/Resume" };
        pauseBtn.style.marginRight = 6;
        controlRow.Add(pauseBtn);

        runtimeManualToggle = new Toggle("Manual Tick") { value = false };
        runtimeManualToggle.style.marginRight = 6;
        runtimeManualToggle.RegisterValueChangedCallback(evt =>
        {
            var runner = GetSelectedRuntimeRunner();
            if (runner == null) return;
            runner.DebugManualTick = evt.newValue;
        });
        controlRow.Add(runtimeManualToggle);

        var stepBtn = new Button(() =>
        {
            var runner = GetSelectedRuntimeRunner();
            runner?.DebugStep();
        })
        { text = "Step" };
        stepBtn.style.marginRight = 6;
        controlRow.Add(stepBtn);

        runtimeDeltaScaleField = new FloatField("Δt x") { value = 1f };
        runtimeDeltaScaleField.style.width = 120;
        runtimeDeltaScaleField.RegisterValueChangedCallback(evt =>
        {
            var runner = GetSelectedRuntimeRunner();
            if (runner == null) return;
            runner.DebugDeltaTimeScale = evt.newValue;
        });
        controlRow.Add(runtimeDeltaScaleField);

        runtimeStateLabel = new Label("Machine: (null)");
        runtimePanel.Add(runtimeStateLabel);

        runtimeHistoryLabel = new Label("History: (empty)");
        runtimePanel.Add(runtimeHistoryLabel);
    }

    private YusWorkflowRunner GetSelectedRuntimeRunner()
    {
        if (runtimeRunners.Count == 0) return null;
        if (runtimeRunnerIndex < 0 || runtimeRunnerIndex >= runtimeRunners.Count) return null;
        return runtimeRunners[runtimeRunnerIndex];
    }

    private void RefreshRuntimeRunnersIfNeeded()
    {
        if (EditorApplication.timeSinceStartup - lastRuntimeScanTime < 0.25) return;
        lastRuntimeScanTime = EditorApplication.timeSinceStartup;

        var oldSelected = runtimeRunners.Count > 0 && runtimeRunnerIndex >= 0 && runtimeRunnerIndex < runtimeRunners.Count
            ? runtimeRunners[runtimeRunnerIndex]
            : null;

        runtimeRunners.Clear();
        var candidates = FindObjectsOfType<YusWorkflowRunner>();
        for (var i = 0; i < candidates.Length; i++)
        {
            var r = candidates[i];
            if (r == null) continue;
            if (r.Workflow != asset) continue;
            runtimeRunners.Add(r);
        }

        if (oldSelected != null)
        {
            var idx = runtimeRunners.IndexOf(oldSelected);
            if (idx >= 0) runtimeRunnerIndex = idx;
        }
    }

    private void UpdateRuntimePanel(YusWorkflowMachine machine)
    {
        if (runtimeInfoBox == null || runtimeRunnerPopup == null || runtimeStateLabel == null || runtimeHistoryLabel == null) return;

        if (!Application.isPlaying)
        {
            runtimeInfoBox.text = "进入 PlayMode 后显示运行状态。";
            runtimeInfoBox.style.display = DisplayStyle.Flex;
            runtimeRunnerPopup.style.display = DisplayStyle.None;
            if (runtimeManualToggle != null) runtimeManualToggle.style.display = DisplayStyle.None;
            if (runtimeDeltaScaleField != null) runtimeDeltaScaleField.style.display = DisplayStyle.None;
            runtimeStateLabel.text = "Machine: (null)";
            runtimeHistoryLabel.text = "History: (empty)";
            return;
        }

        if (asset == null)
        {
            runtimeInfoBox.text = "先选择一个工作流资源。";
            runtimeInfoBox.style.display = DisplayStyle.Flex;
            runtimeRunnerPopup.style.display = DisplayStyle.None;
            if (runtimeManualToggle != null) runtimeManualToggle.style.display = DisplayStyle.None;
            if (runtimeDeltaScaleField != null) runtimeDeltaScaleField.style.display = DisplayStyle.None;
            runtimeStateLabel.text = "Machine: (null)";
            runtimeHistoryLabel.text = "History: (empty)";
            return;
        }

        runtimeRunnerPopup.style.display = DisplayStyle.Flex;
        if (runtimeManualToggle != null) runtimeManualToggle.style.display = DisplayStyle.Flex;
        if (runtimeDeltaScaleField != null) runtimeDeltaScaleField.style.display = DisplayStyle.Flex;

        var options = runtimeRunners.Select(r =>
        {
            if (r == null) return "(null)";
            return $"{r.name} (#{r.GetInstanceID()})";
        }).ToList();

        if (options.Count == 0)
        {
            runtimeInfoBox.text = "场景中没有使用该资源的 YusWorkflowRunner。";
            runtimeInfoBox.style.display = DisplayStyle.Flex;
            runtimeRunnerPopup.choices.Clear();
            runtimeRunnerPopup.choices.Add("(none)");
            runtimeRunnerPopup.SetValueWithoutNotify("(none)");
            runtimeRunnerIndex = 0;
            if (runtimeManualToggle != null) runtimeManualToggle.SetValueWithoutNotify(false);
            if (runtimeDeltaScaleField != null) runtimeDeltaScaleField.SetValueWithoutNotify(1f);
            runtimeStateLabel.text = "Machine: (null)";
            runtimeHistoryLabel.text = "History: (empty)";
            return;
        }

        runtimeInfoBox.style.display = DisplayStyle.None;

        runtimeRunnerPopup.choices.Clear();
        runtimeRunnerPopup.choices.AddRange(options);

        if (runtimeRunnerIndex < 0) runtimeRunnerIndex = 0;
        if (runtimeRunnerIndex >= options.Count) runtimeRunnerIndex = 0;
        runtimeRunnerPopup.SetValueWithoutNotify(options[runtimeRunnerIndex]);

        var selectedRunner = GetSelectedRuntimeRunner();
        if (selectedRunner != null)
        {
            if (runtimeManualToggle != null) runtimeManualToggle.SetValueWithoutNotify(selectedRunner.DebugManualTick);
            if (runtimeDeltaScaleField != null) runtimeDeltaScaleField.SetValueWithoutNotify(selectedRunner.DebugDeltaTimeScale);
        }

        for (var i = 0; i < runtimeRunners.Count; i++)
        {
            var r = runtimeRunners[i];
            if (r == null) continue;
            var id = r.GetInstanceID();
            if (!lastHistoryCountByRunnerId.ContainsKey(id))
            {
                lastHistoryCountByRunnerId[id] = 0;
            }
        }

        runtimeStateLabel.text = machine == null
            ? "Machine: (null)"
            : $"Current: {machine.CurrentNodeGuid}\nPrevious: {machine.PreviousNodeGuid}";

        if (machine == null)
        {
            runtimeHistoryLabel.text = "History: (empty)";
            return;
        }

        var lines = new List<string>();
        var history = machine.TransitionHistory;
        var start = Mathf.Max(0, history.Count - 10);
        for (var i = start; i < history.Count; i++)
        {
            var h = history[i];
            var from = string.IsNullOrEmpty(h.FromGuid) ? "(null)" : h.FromGuid;
            var to = string.IsNullOrEmpty(h.ToGuid) ? "(null)" : h.ToGuid;
            var edgePart = string.IsNullOrEmpty(h.EdgeGuid) ? string.Empty : $" edge={h.EdgeGuid}";
            var portPart = string.IsNullOrEmpty(h.FromPortName) ? string.Empty : $" port={h.FromPortName}";
            lines.Add($"{h.Time:0.00} {h.Reason}: {from} -> {to}{edgePart}{portPart}");
        }

        runtimeHistoryLabel.text = lines.Count == 0 ? "History: (empty)" : string.Join("\n", lines);
    }

    private void DrawNodeInspector(YusWorkflowNodeView nodeView)
    {
        if (asset == null)
        {
            inspectorContent.Add(new Label("请先选择/创建一个工作流资源。"));
            return;
        }

        inspectorContent.Add(new Label("节点"));
        inspectorContent.Add(new Label($"Guid: {nodeView.Guid}"));

        var so = new SerializedObject(asset);
        var nodeProp = FindNodeRecordProperty(so, nodeView.Guid);
        if (nodeProp == null)
        {
            inspectorContent.Add(new HelpBox("在资源中未找到该节点记录。", HelpBoxMessageType.Error));
            return;
        }

        var managedRef = nodeProp.FindPropertyRelative("Node");
        if (managedRef == null)
        {
            inspectorContent.Add(new HelpBox("节点引用数据丢失(SerializeReference)。", HelpBoxMessageType.Error));
            return;
        }

        // Rename/title override (stored inside node instance).
        var titleProp = managedRef.FindPropertyRelative("titleOverride");
        if (titleProp != null)
        {
            inspectorContent.Add(new Label("标题"));
            var titleField = new TextField { value = titleProp.stringValue ?? string.Empty };
            titleField.RegisterValueChangedCallback(evt =>
            {
                so.Update();
                titleProp.stringValue = evt.newValue;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                graphView?.RefreshNodeTitle(nodeView.Guid);
            });
            inspectorContent.Add(titleField);
        }

        var nodeTypes = YusWorkflowEditorTypeUtil.GetNodeTypes();
        var names = nodeTypes.Select(YusWorkflowEditorTypeUtil.GetNodeDisplayName).ToList();

        var currentType = nodeView.NodeType;
        var currentIndex = Mathf.Max(0, currentType == null ? 0 : nodeTypes.IndexOf(currentType));

        inspectorContent.Add(new Label("节点类型"));
        var popup = new PopupField<string>(names, currentIndex);
        popup.RegisterValueChangedCallback(evt =>
        {
            var index = names.IndexOf(evt.newValue);
            if (index < 0 || index >= nodeTypes.Count) return;
            graphView.ChangeNodeType(nodeView.Guid, nodeTypes[index]);
        });
        inspectorContent.Add(popup);

        inspectorContent.Add(new Button(() => graphView.SetEntry(nodeView.Guid)) { text = "设为入口" });
        inspectorContent.Add(new Button(() => YusWorkflowNodeGeneratorWindow.Open()) { text = "创建新节点类型(生成器)" });
        if (asset.EntryNodeGuid == nodeView.Guid)
        {
            inspectorContent.Add(new Label("(当前入口)"));
        }

        inspectorContent.Add(new Label("参数"));
        var field = new PropertyField(managedRef);
        field.Bind(so);
        inspectorContent.Add(field);
    }

    private void DrawEdgeInspector(Edge edge)
    {
        if (asset == null)
        {
            inspectorContent.Add(new Label("请先选择/创建一个工作流资源。"));
            return;
        }

        if (edge?.userData is not YusWorkflowEdgeUserData data)
        {
            inspectorContent.Add(new HelpBox("连线缺少工作流数据。", HelpBoxMessageType.Error));
            return;
        }

        inspectorContent.Add(new Label("连线"));
        inspectorContent.Add(new Label($"Guid: {data.EdgeGuid}"));

        var conditionTypes = YusWorkflowEditorTypeUtil.GetConditionTypes();
        var conditionNames = new List<string> { "(永真)" };
        conditionNames.AddRange(conditionTypes.Select(t => t.Name));

        var so = new SerializedObject(asset);
        var edgeProp = FindEdgeRecordProperty(so, data.EdgeGuid);
        if (edgeProp == null)
        {
            inspectorContent.Add(new HelpBox("在资源中未找到该连线记录。", HelpBoxMessageType.Error));
            return;
        }

        var condProp = edgeProp.FindPropertyRelative("Condition");
        if (condProp == null)
        {
            inspectorContent.Add(new HelpBox("连线条件数据丢失(SerializeReference)。", HelpBoxMessageType.Error));
            return;
        }

        var currentConditionType = condProp.managedReferenceValue?.GetType();
        var currentIndex = 0;
        if (currentConditionType != null)
        {
            var idx = conditionTypes.IndexOf(currentConditionType);
            if (idx >= 0) currentIndex = idx + 1;
        }

        inspectorContent.Add(new Label("条件类型"));
        var popup = new PopupField<string>(conditionNames, currentIndex);
        popup.RegisterValueChangedCallback(evt =>
        {
            so.Update();
            if (evt.newValue == "(永真)")
            {
                condProp.managedReferenceValue = null;
            }
            else
            {
                var idx = conditionNames.IndexOf(evt.newValue) - 1;
                if (idx >= 0 && idx < conditionTypes.Count)
                {
                    condProp.managedReferenceValue = Activator.CreateInstance(conditionTypes[idx]);
                }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        });
        inspectorContent.Add(popup);

        inspectorContent.Add(new Label("条件参数"));
        var field = new PropertyField(condProp);
        field.Bind(so);
        inspectorContent.Add(field);
    }

    private static SerializedProperty FindNodeRecordProperty(SerializedObject so, string guid)
    {
        var list = so.FindProperty("nodes");
        if (list == null || !list.isArray) return null;

        for (var i = 0; i < list.arraySize; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            var guidProp = element.FindPropertyRelative("Guid");
            if (guidProp != null && guidProp.stringValue == guid) return element;
        }

        return null;
    }

    private static SerializedProperty FindEdgeRecordProperty(SerializedObject so, string guid)
    {
        var list = so.FindProperty("edges");
        if (list == null || !list.isArray) return null;

        for (var i = 0; i < list.arraySize; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            var guidProp = element.FindPropertyRelative("Guid");
            if (guidProp != null && guidProp.stringValue == guid) return element;
        }

        return null;
    }
}

internal static class YusWorkflowEditorTypeUtil
{
    private static readonly List<Type> CachedNodeTypes = new List<Type>();
    private static readonly List<Type> CachedConditionTypes = new List<Type>();
    private static double lastRefresh;

    public static List<Type> GetNodeTypes()
    {
        RefreshIfNeeded();
        return CachedNodeTypes;
    }

    public static List<Type> GetConditionTypes()
    {
        RefreshIfNeeded();
        return CachedConditionTypes;
    }

    public static string GetNodeDisplayName(Type type)
    {
        if (type == null) return "(null)";
        var attr = Attribute.GetCustomAttribute(type, typeof(YusWorkflowNodeMenuAttribute)) as YusWorkflowNodeMenuAttribute;
        if (attr != null && !string.IsNullOrWhiteSpace(attr.Path))
        {
            return $"{attr.Path} ({type.Name})";
        }
        return type.Name;
    }

    private static void RefreshIfNeeded()
    {
        if (EditorApplication.timeSinceStartup - lastRefresh < 2) return;
        lastRefresh = EditorApplication.timeSinceStartup;

        CachedNodeTypes.Clear();
        CachedConditionTypes.Clear();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                continue;
            }

            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type == null) continue;
                if (type.IsAbstract) continue;
                if (type.IsGenericTypeDefinition) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                if (typeof(YusWorkflowNode).IsAssignableFrom(type))
                {
                    CachedNodeTypes.Add(type);
                    continue;
                }

                if (typeof(YusWorkflowCondition).IsAssignableFrom(type))
                {
                    CachedConditionTypes.Add(type);
                }
            }
        }

        CachedNodeTypes.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
        CachedConditionTypes.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
    }
}
