using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class YusAnimFSMGenerator : EditorWindow
{
    private AnimatorController animController;
    private string className = "Player"; // 生成的类名前缀，如 Player
    private string savePath = "Assets/YusGameFrame/AnimSystem/Anims/";

    [MenuItem("Tools/Yus Data/G. 动画状态机生成器 (Anim To FSM)")]
    public static void ShowWindow()
    {
        GetWindow<YusAnimFSMGenerator>("Anim To FSM");
    }

    private void OnGUI()
    {
        GUILayout.Label("🤖 Animator -> FSM 代码生成器", EditorStyles.boldLabel);
        
        animController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animController, typeof(AnimatorController), false);
        className = EditorGUILayout.TextField("生成类名前缀:", className);
        
        if (GUILayout.Button("选择保存路径"))
        {
            string path = EditorUtility.OpenFolderPanel("选择保存文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为相对路径
                if (path.StartsWith(Application.dataPath))
                    savePath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
            }
        }
        GUILayout.Label($"保存路径: {savePath}");

        EditorGUILayout.Space();

        if (GUILayout.Button("🚀 生成代码 & SO", GUILayout.Height(40)))
        {
            if (animController == null) { ShowNotification(new GUIContent("请拖入 Animator!")); return; }
            Generate();
        }
    }

    private void Generate()
    {
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        // 1. 生成 Config SO
        AnimatorConfigSO config = ScriptableObject.CreateInstance<AnimatorConfigSO>();
        
        // 获取 Base Layer 的所有状态
        var rootStateMachine = animController.layers[0].stateMachine;
        var states = rootStateMachine.states;

        // 填充 SO 数据
        foreach (var s in states)
        {
            config.states.Add(new AnimatorConfigSO.StateInfo 
            { 
                stateName = s.state.name, 
                hash = Animator.StringToHash(s.state.name) 
            });
        }
        foreach (var p in animController.parameters)
        {
            config.parameters.Add(new AnimatorConfigSO.ParamInfo
            {
                paramName = p.name,
                hash = p.nameHash,
                type = p.type
            });
        }

        string soPath = savePath + className + "AnimConfig.asset";
        AssetDatabase.CreateAsset(config, soPath);

        // 2. 生成 C# 代码
        GenerateScripts(states, className);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("成功", $"生成完毕！\nSO: {soPath}\nCode: {savePath}", "OK");
    }

    private void GenerateScripts(ChildAnimatorState[] states, string prefix)
    {
        StringBuilder sb = new StringBuilder();
        string controllerName = prefix + "Controller"; // e.g. PlayerController

        // --- 头部引用 ---
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("");

        // --- 1. 控制器部分 (Controller) ---
        sb.AppendLine($"[RequireComponent(typeof(Animator))]");
        sb.AppendLine($"public partial class {controllerName} : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine($"    public YusFSM<{controllerName}> fsm;");
        sb.AppendLine("    public Animator Animator { get; private set; }");
        sb.AppendLine($"    // 对应生成的 SO 路径: Resources/{prefix}AnimConfig");
        sb.AppendLine("    // 这里简单处理，你可以用 YusResManager 加载");
        sb.AppendLine("");
        sb.AppendLine("    private void Awake()");
        sb.AppendLine("    {");
        sb.AppendLine("        Animator = GetComponent<Animator>();");
        sb.AppendLine($"        fsm = new YusFSM<{controllerName}>(this);");
        sb.AppendLine("        OnInit();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update() => fsm.OnUpdate();");
        sb.AppendLine("    private void FixedUpdate() => fsm.OnFixedUpdate();");
        sb.AppendLine("    partial void OnInit(); // 用户自定义初始化钩子");
        sb.AppendLine("}");
        sb.AppendLine("");

        // --- 2. 状态类部分 (States) ---
        foreach (var childState in states)
        {
            string rawStateName = childState.state.name; // e.g. "Idle"
            string stateClassName = prefix + rawStateName + "State"; // e.g. PlayerIdleState
            int hash = Animator.StringToHash(rawStateName);

            sb.AppendLine($"// 状态: {rawStateName}");
            sb.AppendLine($"public partial class {stateClassName} : YusState<{controllerName}>");
            sb.AppendLine("{");
            
            // OnEnter: 自动播放动画
            sb.AppendLine("    public override void OnEnter()");
            sb.AppendLine("    {");
            sb.AppendLine($"        // 自动播放动画: {rawStateName}");
            sb.AppendLine($"        owner.Animator.CrossFade({hash}, 0.1f);"); 
            sb.AppendLine("        OnEnterUser();");
            sb.AppendLine("    }");
            sb.AppendLine("");
            
            // 用户扩展钩子
            sb.AppendLine("    partial void OnEnterUser();");
            sb.AppendLine("    public override void OnUpdate() { OnUpdateUser(); }");
            sb.AppendLine("    partial void OnUpdateUser();");
            
            sb.AppendLine("}");
            sb.AppendLine("");
        }

        File.WriteAllText(savePath + controllerName + "_Gen.cs", sb.ToString(), Encoding.UTF8);
    }
}