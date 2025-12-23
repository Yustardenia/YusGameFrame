using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public sealed class YusWorkflowSearchWindowProvider : ScriptableObject, ISearchWindowProvider
{
    private YusWorkflowGraphView graphView;
    private EditorWindow editorWindow;

    public void Init(YusWorkflowGraphView graphView, EditorWindow editorWindow)
    {
        this.graphView = graphView;
        this.editorWindow = editorWindow;
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("添加节点"), 0)
        };

        var types = YusWorkflowEditorTypeUtil.GetNodeTypes();
        foreach (var type in types)
        {
            if (type == null) continue;

            var path = GetMenuPath(type);
            var segments = string.IsNullOrWhiteSpace(path)
                ? new[] { type.Name }
                : path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            var level = 1;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                var groupPath = string.Join("/", segments.Take(i + 1));
                AddGroupIfMissing(tree, groupPath, segments[i], level);
                level++;
            }

            var leafName = segments.Length == 0 ? type.Name : segments[^1];
            tree.Add(new SearchTreeEntry(new GUIContent(leafName))
            {
                level = Mathf.Max(1, segments.Length),
                userData = type
            });
        }

        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
        if (graphView == null) return false;
        if (entry?.userData is not Type type) return false;

        var graphMousePosition = graphView.ScreenToGraphPosition(context.screenMousePosition, editorWindow);
        graphView.CreateNodeAt(graphMousePosition, type);
        return true;
    }

    private static string GetMenuPath(Type type)
    {
        var attr = Attribute.GetCustomAttribute(type, typeof(YusWorkflowNodeMenuAttribute)) as YusWorkflowNodeMenuAttribute;
        if (attr == null) return type.Name;
        return string.IsNullOrWhiteSpace(attr.Path) ? type.Name : attr.Path.Trim();
    }

    private static void AddGroupIfMissing(List<SearchTreeEntry> tree, string groupPath, string groupName, int level)
    {
        for (var i = 0; i < tree.Count; i++)
        {
            var existing = tree[i] as SearchTreeGroupEntry;
            if (existing == null) continue;
            if (existing.level != level) continue;
            if (existing.content.text == groupName) return;
        }

        tree.Add(new SearchTreeGroupEntry(new GUIContent(groupName), level));
    }
}
