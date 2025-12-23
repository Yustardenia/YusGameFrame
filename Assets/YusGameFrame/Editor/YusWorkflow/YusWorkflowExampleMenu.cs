using UnityEditor;
using UnityEngine;

public static class YusWorkflowExampleMenu
{
    [MenuItem(YusGameFrameEditorMenu.Root + "Workflow/示例/生成自动贩卖机工作流")]
    public static void CreateVendingMachineWorkflow()
    {
        const string path = "Assets/YusGameFrame/YusWorkflow/Example/VendingMachineWorkflow.asset";

        var asset = AssetDatabase.LoadAssetAtPath<YusWorkflowAsset>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<YusWorkflowAsset>();
            AssetDatabase.CreateAsset(asset, path);
        }

        // Reset via SerializedObject so we can clear lists cleanly.
        var so = new SerializedObject(asset);
        so.FindProperty("entryNodeGuid").stringValue = string.Empty;
        so.FindProperty("nodes").arraySize = 0;
        so.FindProperty("edges").arraySize = 0;
        so.ApplyModifiedPropertiesWithoutUndo();

        var initGuid = System.Guid.NewGuid().ToString("N");
        var coinGuid = System.Guid.NewGuid().ToString("N");
        var dispenseGuid = System.Guid.NewGuid().ToString("N");
        var endGuid = System.Guid.NewGuid().ToString("N");

        asset.Editor_AddNode(new YusWorkflowAsset.NodeRecord
        {
            Guid = initGuid,
            Position = new Vector2(100, 220),
            Node = new VMNode_Init()
        });

        asset.Editor_AddNode(new YusWorkflowAsset.NodeRecord
        {
            Guid = coinGuid,
            Position = new Vector2(420, 220),
            Node = new VMNode_Coin()
        });

        asset.Editor_AddNode(new YusWorkflowAsset.NodeRecord
        {
            Guid = dispenseGuid,
            Position = new Vector2(760, 140),
            Node = new VMNode_Dispense()
        });

        asset.Editor_AddNode(new YusWorkflowAsset.NodeRecord
        {
            Guid = endGuid,
            Position = new Vector2(760, 340),
            Node = new VMNode_End()
        });

        asset.Editor_AddEdge(new YusWorkflowAsset.EdgeRecord
        {
            Guid = System.Guid.NewGuid().ToString("N"),
            FromNodeGuid = initGuid,
            FromPortName = "Next",
            ToNodeGuid = coinGuid,
            Condition = new VMCond_HasSelection()
        });

        asset.Editor_AddEdge(new YusWorkflowAsset.EdgeRecord
        {
            Guid = System.Guid.NewGuid().ToString("N"),
            FromNodeGuid = coinGuid,
            FromPortName = "Paid",
            ToNodeGuid = dispenseGuid,
            Condition = new VMCond_PaidEnough()
        });

        asset.Editor_AddEdge(new YusWorkflowAsset.EdgeRecord
        {
            Guid = System.Guid.NewGuid().ToString("N"),
            FromNodeGuid = coinGuid,
            FromPortName = "Cancel",
            ToNodeGuid = endGuid,
            Condition = new VMCond_Cancel()
        });

        asset.Editor_AddEdge(new YusWorkflowAsset.EdgeRecord
        {
            Guid = System.Guid.NewGuid().ToString("N"),
            FromNodeGuid = dispenseGuid,
            FromPortName = "Next",
            ToNodeGuid = endGuid,
            Condition = new VMCond_DispenseDone()
        });

        asset.Editor_AddEdge(new YusWorkflowAsset.EdgeRecord
        {
            Guid = System.Guid.NewGuid().ToString("N"),
            FromNodeGuid = endGuid,
            FromPortName = "Next",
            ToNodeGuid = initGuid,
            Condition = new VMCond_Restart()
        });

        asset.Editor_SetEntry(initGuid);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(asset);
    }
}
