using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class YusWorkflowNodeGeneratorWindow : EditorWindow
{
    private const string DefaultFolder = "Assets/YusGameFrame/YusWorkflow/Generated/Runtime";

    private string folder = DefaultFolder;
    private string @namespace = string.Empty;
    private string className = "MyWorkflowNode";
    private string menuPath = "Workflow/My Node";
    private string outputPortsCsv = "Next";

    [MenuItem(YusGameFrameEditorMenu.Root + "Workflow/生成器/创建节点脚本")]
    public static void Open()
    {
        GetWindow<YusWorkflowNodeGeneratorWindow>("工作流节点生成器");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("生成 2 个文件：", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("- *.g.cs（自动生成，不要手改）");
        EditorGUILayout.LabelField("- *.Custom.cs（你的 partial，写逻辑在这里）");

        EditorGUILayout.Space();

        folder = EditorGUILayout.TextField("保存目录", folder);
        @namespace = EditorGUILayout.TextField("命名空间(可选)", @namespace);
        className = EditorGUILayout.TextField("类名", className);
        menuPath = EditorGUILayout.TextField("菜单路径", menuPath);
        outputPortsCsv = EditorGUILayout.TextField("输出端口(csv)", outputPortsCsv);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(className)))
        {
            if (GUILayout.Button("生成"))
            {
                Generate();
            }
        }
    }

    private void Generate()
    {
        var trimmedClass = (className ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedClass))
        {
            EditorUtility.DisplayDialog("工作流生成器", "类名不能为空。", "确定");
            return;
        }

        if (!IsValidIdentifier(trimmedClass))
        {
            EditorUtility.DisplayDialog("工作流生成器", "类名必须是合法的 C# 标识符。", "确定");
            return;
        }

        var trimmedFolder = string.IsNullOrWhiteSpace(folder) ? DefaultFolder : folder.Trim();
        if (!trimmedFolder.StartsWith("Assets/", StringComparison.Ordinal) &&
            !string.Equals(trimmedFolder, "Assets", StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("工作流生成器", "保存目录必须在 Assets/ 下。", "确定");
            return;
        }

        Directory.CreateDirectory(trimmedFolder);

        var generatedPath = Path.Combine(trimmedFolder, $"{trimmedClass}.g.cs").Replace('\\', '/');
        var customPath = Path.Combine(trimmedFolder, $"{trimmedClass}.Custom.cs").Replace('\\', '/');

        if (File.Exists(generatedPath) || File.Exists(customPath))
        {
            if (!EditorUtility.DisplayDialog(
                    "工作流生成器",
                    $"文件已存在：\n{generatedPath}\n{customPath}\n\n是否覆盖？",
                    "覆盖",
                    "取消"))
            {
                return;
            }
        }

        var ports = ParsePorts(outputPortsCsv);
        if (ports.Length == 0) ports = new[] { "Next" };

        File.WriteAllText(generatedPath, BuildGenerated(trimmedClass, @namespace, menuPath, ports), new UTF8Encoding(false));
        if (!File.Exists(customPath))
        {
            File.WriteAllText(customPath, BuildCustom(trimmedClass, @namespace), new UTF8Encoding(false));
        }

        AssetDatabase.Refresh();
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(customPath);
        if (obj != null) EditorGUIUtility.PingObject(obj);
    }

    private static string[] ParsePorts(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();
        var parts = csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].Trim();
        }
        return parts;
    }

    private static bool IsValidIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (!(char.IsLetter(value[0]) || value[0] == '_')) return false;
        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
        }
        return true;
    }

    private static string BuildGenerated(string className, string ns, string menuPath, string[] ports)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();

        var hasNamespace = !string.IsNullOrWhiteSpace(ns);
        if (hasNamespace)
        {
            sb.Append("namespace ").Append(ns.Trim()).AppendLine();
            sb.AppendLine("{");
        }

        sb.AppendLine("[Serializable]");
        sb.Append("[YusWorkflowNodeMenu(\"").Append(Escape(menuPath)).AppendLine("\")]");
        sb.Append("public partial class ").Append(className).AppendLine(" : YusWorkflowNode");
        sb.AppendLine("{");
        sb.AppendLine("    public override IEnumerable<string> GetOutputPortNames()");
        sb.AppendLine("    {");
        for (var i = 0; i < ports.Length; i++)
        {
            var port = ports[i];
            if (string.IsNullOrWhiteSpace(port)) continue;
            sb.Append("        yield return \"").Append(Escape(port.Trim())).AppendLine("\";");
        }
        sb.AppendLine("    }");
        sb.AppendLine("}");

        if (hasNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string BuildCustom(string className, string ns)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();

        var hasNamespace = !string.IsNullOrWhiteSpace(ns);
        if (hasNamespace)
        {
            sb.Append("namespace ").Append(ns.Trim()).AppendLine();
            sb.AppendLine("{");
        }

        sb.Append("public partial class ").Append(className).AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("    protected override void OnEnter()");
        sb.AppendLine("    {");
        sb.AppendLine("        // You can access workflow runtime data via `Context`.");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    protected override void OnUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        if (hasNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string Escape(string s) => (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
}
