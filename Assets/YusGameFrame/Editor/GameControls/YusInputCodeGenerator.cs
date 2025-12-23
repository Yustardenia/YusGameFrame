using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class YusInputCodeGenerator : EditorWindow
{
    private string scriptName = "PlayerController";
    private string mapName = "Gameplay";

    [MenuItem(YusGameFrameEditorMenu.Root + "Generators/Input/Input Generator")]
    public static void ShowWindow()
    {
        GetWindow<YusInputCodeGenerator>("Input Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Input Controller Generator", EditorStyles.boldLabel);

        scriptName = EditorGUILayout.TextField("Script Name", scriptName);
        mapName = EditorGUILayout.TextField("ActionMap Name", mapName);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Generates a controller template:\n1) Vector2 actions cache input\n2) Button actions fire on performed\n3) Uses YusInputManager.GetAction(map, action) to avoid direct controls coupling",
            MessageType.Info);

        if (GUILayout.Button("Generate", GUILayout.Height(40)))
        {
            GenerateScript();
        }
    }

    private void GenerateScript()
    {
        var controls = new GameControls();
        InputActionMap map = controls.asset.FindActionMap(mapName, false);

        if (map == null)
        {
            EditorUtility.DisplayDialog("Error", $"ActionMap '{mapName}' not found. Check your Input Actions and C# generation.", "OK");
            return;
        }

        var sb = new StringBuilder();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated input controller template.");
        sb.AppendLine("/// For dialogue/cutscene, consider `using (YusInputManager.Instance.AcquireUI()) { ... }`.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {scriptName} : MonoBehaviour");
        sb.AppendLine("{");

        var cachedVectorActions = new List<string>();
        var fieldSb = new StringBuilder();

        foreach (var action in map.actions)
        {
            string type = GetActionType(action);
            if (type == "Vector2")
            {
                cachedVectorActions.Add(action.name);
                fieldSb.AppendLine($"    [SerializeField] private Vector2 _input{action.name};");
            }
        }

        if (cachedVectorActions.Count > 0)
        {
            sb.AppendLine("    [Header(\"Input Cache\")]");
            sb.Append(fieldSb);
        }
        sb.AppendLine("");

        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        var input = YusInputManager.Instance;");
        sb.AppendLine("        if (input == null) return;");
        sb.AppendLine("");
        sb.AppendLine("        // Auto-register inputs (auto-unregisters on destroy)");

        foreach (var action in map.actions)
        {
            sb.AppendLine($"        this.YusRegisterInput(input.GetAction(\"{mapName}\", \"{action.name}\"), On{action.name});");
        }

        sb.AppendLine("    }");
        sb.AppendLine("");

        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: non-physics logic");
        sb.AppendLine("    }");
        sb.AppendLine("");

        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        if (cachedVectorActions.Count > 0)
        {
            sb.AppendLine("        // TODO: physics movement");
            foreach (var name in cachedVectorActions)
                sb.AppendLine($"        // if (_input{name} != Vector2.zero) {{ ... }}");
        }
        else
        {
            sb.AppendLine("        // TODO: physics logic");
        }
        sb.AppendLine("    }");
        sb.AppendLine("");

        foreach (var action in map.actions)
        {
            string methodName = $"On{action.name}";
            string type = GetActionType(action);

            sb.AppendLine($"    // Action: {action.name} ({type})");
            sb.AppendLine($"    private void {methodName}(InputAction.CallbackContext ctx)");
            sb.AppendLine("    {");

            if (type == "Vector2")
            {
                sb.AppendLine($"        _input{action.name} = ctx.ReadValue<Vector2>();");
            }
            else if (type == "Button")
            {
                sb.AppendLine("        if (ctx.performed)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log(\"{action.name} Performed\");");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine("        // TODO: handle custom value type");
            }

            sb.AppendLine("    }");
            sb.AppendLine("");
        }

        sb.AppendLine("}");

        string path = Application.dataPath + $"/YusGameFrame/GameControls/Controllers/{scriptName}.cs";
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"Generated: {path}");
    }

    private static string GetActionType(InputAction action)
    {
        string expected = action.expectedControlType;
        if (!string.IsNullOrEmpty(expected))
        {
            if (expected.Equals("Vector2", StringComparison.OrdinalIgnoreCase)) return "Vector2";
            if (expected.Equals("Button", StringComparison.OrdinalIgnoreCase)) return "Button";
            return expected;
        }

        if (action.type == InputActionType.Button) return "Button";

        if (action.type == InputActionType.Value)
        {
            if (action.name.Contains("Move", StringComparison.OrdinalIgnoreCase) ||
                action.name.Contains("Look", StringComparison.OrdinalIgnoreCase))
                return "Vector2";
        }

        return "Unknown";
    }
}
