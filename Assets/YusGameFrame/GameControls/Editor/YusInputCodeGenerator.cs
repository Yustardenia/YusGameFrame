using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public class YusInputCodeGenerator : EditorWindow
{
    private string scriptName = "PlayerController";
    private string mapName = "Gameplay"; 

    [MenuItem("Tools/Yus Data/E. 输入脚本生成器 (Input Generator)")]
    public static void ShowWindow()
    {
        GetWindow<YusInputCodeGenerator>("输入脚本生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("📝 智能控制器生成器 v2.3 (终极版)", EditorStyles.boldLabel);
        
        scriptName = EditorGUILayout.TextField("生成脚本名:", scriptName);
        mapName = EditorGUILayout.TextField("ActionMap 名称:", mapName);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("功能清单：\n1. 包含最佳实践注释 & TODO\n2. 智能识别 Vector2/Button\n3. 自动生成 Update/FixedUpdate 模板", MessageType.Info);

        if (GUILayout.Button("🚀 生成脚本", GUILayout.Height(40)))
        {
            GenerateScript();
        }
    }

    private void GenerateScript()
    {
        var controls = new GameControls(); 
        InputActionMap map = controls.asset.FindActionMap(mapName);

        if (map == null)
        {
            EditorUtility.DisplayDialog("错误", $"找不到名为 '{mapName}' 的 ActionMap。\n请检查 Input Actions 是否已保存并生成 C#。", "OK");
            return;
        }

        StringBuilder sb = new StringBuilder();

        // --- 1. 引用与注意事项 (已找回！) ---
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// 自动生成的输入控制器");
        sb.AppendLine("/// [注意事项]:");
        sb.AppendLine("/// 1. 持续性动作(移动)应读取输入缓存，逻辑放入 FixedUpdate (物理) 或 Update");
        sb.AppendLine("/// 2. 瞬发类动作(跳跃/攻击)可在回调中直接写逻辑");
        sb.AppendLine("/// 3. 对话/过场时，请调用 YusInputManager.Instance.EnableUI() 锁住操作");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {scriptName} : MonoBehaviour");
        sb.AppendLine("{");

        // --- 2. 自动生成缓存变量 (防报错逻辑) ---
        List<string> cachedVectorNames = new List<string>();
        StringBuilder fieldSb = new StringBuilder();

        foreach (var action in map.actions)
        {
            string type = GetActionType(action);
            
            if (type == "Vector2")
            {
                string varName = $"_input{action.name}";
                cachedVectorNames.Add(action.name);
                fieldSb.AppendLine($"    [SerializeField] private Vector2 {varName};");
            }
        }

        // 只有当有变量时才加 Header，防止报错
        if (cachedVectorNames.Count > 0)
        {
            sb.AppendLine("    [Header(\"Input Cache\")]");
            sb.Append(fieldSb.ToString());
        }
        sb.AppendLine("");

        // --- 3. Start 注册 ---
        sb.AppendLine("    void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        // 自动注册输入事件 (物体销毁自动解绑)");
        foreach (var action in map.actions)
        {
            sb.AppendLine($"        this.YusRegisterInput(YusInputManager.Instance.controls.{mapName}.{action.name}, On{action.name});");
        }
        sb.AppendLine("    }");
        sb.AppendLine("");

        // --- 4. 生命周期模板 (带 TODO) ---
        sb.AppendLine("    void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: 处理非物理逻辑 (如动画状态机参数更新)");
        sb.AppendLine("    }");
        sb.AppendLine("");
        
        sb.AppendLine("    void FixedUpdate()");
        sb.AppendLine("    {");
        if (cachedVectorNames.Count > 0)
        {
            sb.AppendLine("        // TODO: 处理物理移动 (Rigidbody)");
            foreach (var vecName in cachedVectorNames)
                sb.AppendLine($"        // if (_input{vecName} != Vector2.zero) {{ ... }}");
        }
        else
        {
            sb.AppendLine("        // TODO: 处理物理逻辑");
        }
        sb.AppendLine("    }");
        sb.AppendLine("");

        // --- 5. 回调函数生成 ---
        foreach (var action in map.actions)
        {
            string methodName = $"On{action.name}";
            string type = GetActionType(action);

            sb.AppendLine($"    // Action: {action.name} ({type})");
            sb.AppendLine($"    private void {methodName}(InputAction.CallbackContext ctx)");
            sb.AppendLine("    {");
            
            if (type == "Vector2")
            {
                sb.AppendLine($"        // [持续性] 更新缓存");
                sb.AppendLine($"        _input{action.name} = ctx.ReadValue<Vector2>();");
            }
            else if (type == "Button")
            {
                sb.AppendLine("        // [瞬发] 按下瞬间执行");
                sb.AppendLine("        if (ctx.performed)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log(\"{action.name} Performed\");");
                sb.AppendLine("            // TODO: 执行逻辑");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine($"        // 类型: {type}，请手动处理");
                sb.AppendLine($"        // var val = ctx.ReadValue<float>();");
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("");
        }

        sb.AppendLine("}");

        // --- 写入文件 ---
        string path = Application.dataPath + $"/YusGameFrame/GameControls/Controllers/{scriptName}.cs";
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();
        
        Debug.Log($"<color=green>脚本已生成: {path}</color>");
        EditorUtility.OpenWithDefaultApp(path); 
    }

    // --- 智能类型推断 ---
    private string GetActionType(InputAction action)
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
            if (action.name.Contains("Move") || action.name.Contains("Look")) return "Vector2";
        }

        return "Unknown";
    }
}