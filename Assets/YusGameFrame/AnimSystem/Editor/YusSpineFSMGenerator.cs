using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

// 注意：这里不能直接引用 Spine，否则没包会报错
// 我们把引用放在宏定义里面
#if YUS_SPINE
using Spine.Unity;
#endif

public class YusSpineFSMGenerator : EditorWindow
{
    // 定义我们专用的宏符号
    private const string SYMBOL_NAME = "YUS_SPINE";

    private string className = "Player";
    private string savePath = "Assets/YusGameFrame/AnimSystem/SpineAnims/";

    // 只有定义了宏，才声明 Spine 类型的变量
#if YUS_SPINE
    private SkeletonDataAsset spineData; 
#else
    private Object rawSpineData; // 没宏的时候用 Object 占位，防止界面报错
#endif

    [MenuItem("Tools/Yus Data/9. Spine FSM 生成器")]
    public static void ShowWindow()
    {
        GetWindow<YusSpineFSMGenerator>("Spine To FSM");
    }

    private void OnGUI()
    {
        GUILayout.Label("🦕 Spine -> FSM 代码生成器", EditorStyles.boldLabel);

        // --- 1. 检查宏定义状态 ---
        if (!IsSymbolDefined())
        {
            DrawSymbolSetupGUI();
            return; // 如果没配置好，不显示后续界面
        }

        // --- 2. 正常的生成界面 (只有配置好宏才会运行到这里) ---
        DrawGeneratorGUI();
    }

    // =========================================================
    // Part A: 环境配置引导 (处理依赖)
    // =========================================================

    private bool IsSymbolDefined()
    {
#if YUS_SPINE
        return true;
#else
        return false;
#endif
    }

    private void DrawSymbolSetupGUI()
    {
        EditorGUILayout.HelpBox(
            "检测到未开启 Spine 支持。\n" +
            "如果你的项目中已经导入了 Spine 插件，请点击下方按钮开启此功能。\n" +
            "如果没有导入 Spine，请忽略此工具。", 
            MessageType.Warning);

        if (GUILayout.Button("✅ 我已安装 Spine，开启此工具"))
        {
            AddScriptingDefineSymbol(SYMBOL_NAME);
        }
    }

    private void AddScriptingDefineSymbol(string symbol)
    {
        // 获取当前构建目标 (Standalone, Android, iOS...)
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        
        // 获取现有的 Symbols
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

        if (!defines.Contains(symbol))
        {
            if (string.IsNullOrEmpty(defines)) defines = symbol;
            else defines += ";" + symbol;

            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
            Debug.Log($"已添加宏定义: {symbol}，正在重新编译...");
        }
    }

    // =========================================================
    // Part B: 核心生成逻辑 (包裹在宏里)
    // =========================================================

    private void DrawGeneratorGUI()
    {
#if YUS_SPINE
        spineData = (SkeletonDataAsset)EditorGUILayout.ObjectField("Skeleton Data", spineData, typeof(SkeletonDataAsset), false);
        className = EditorGUILayout.TextField("生成类名前缀:", className);

        // 路径选择逻辑
        if (GUILayout.Button("选择保存路径"))
        {
            string path = EditorUtility.OpenFolderPanel("选择保存文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                    savePath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
            }
        }
        GUILayout.Label($"保存路径: {savePath}");

        EditorGUILayout.Space();

        if (GUILayout.Button("🚀 生成代码"))
        {
            if (spineData == null) { ShowNotification(new GUIContent("请拖入 SkeletonDataAsset!")); return; }
            Generate();
        }
#endif
    }

#if YUS_SPINE
    private void Generate()
    {
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        // 获取 Spine 数据
        var skeletonData = spineData.GetSkeletonData(true);
        if (skeletonData == null)
        {
            Debug.LogError("无法读取 SkeletonData，请检查 Spine 资源是否正常。");
            return;
        }

        var animations = skeletonData.Animations;
        string controllerName = className + "Controller";
        StringBuilder sb = new StringBuilder();

        // --- 1. 头部引用 ---
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using Spine.Unity;"); // 这里生成的代码肯定需要 Spine
        sb.AppendLine("");

        // --- 2. Controller 类 ---
        sb.AppendLine($"[RequireComponent(typeof(SkeletonAnimation))]");
        sb.AppendLine($"public partial class {controllerName} : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine($"    public YusFSM<{controllerName}> fsm;");
        sb.AppendLine("    public SkeletonAnimation SpineAnim { get; private set; }");
        sb.AppendLine("");
        sb.AppendLine("    private void Awake()");
        sb.AppendLine("    {");
        sb.AppendLine("        SpineAnim = GetComponent<SkeletonAnimation>();");
        sb.AppendLine($"        fsm = new YusFSM<{controllerName}>(this);");
        sb.AppendLine("        OnInit();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update() => fsm.OnUpdate();");
        sb.AppendLine("    private void FixedUpdate() => fsm.OnFixedUpdate();");
        sb.AppendLine("");
        sb.AppendLine("    partial void OnInit();");
        sb.AppendLine("}");
        sb.AppendLine("");

        // --- 3. State 类生成 ---
        foreach (var anim in animations)
        {
            // 处理动画名中的非法字符 (空格、横杠转下划线)
            string safeAnimName = anim.Name.Replace(" ", "_").Replace("-", "_");
            string stateClassName = className + safeAnimName + "State";

            sb.AppendLine($"// Spine Anim: {anim.Name}");
            sb.AppendLine($"public partial class {stateClassName} : YusState<{controllerName}>");
            sb.AppendLine("{");
            
            // OnEnter
            sb.AppendLine("    public override void OnEnter()");
            sb.AppendLine("    {");
            sb.AppendLine("        // 默认循环播放 (true)，如需单次播放请在 Logic 脚本中重写 OnEnterUser");
            // 注意：这里用了 anim.Name (原始字符串) 去播放，用 safeAnimName 做类名
            sb.AppendLine($"        owner.SpineAnim.AnimationState.SetAnimation(0, \"{anim.Name}\", true);");
            sb.AppendLine("        OnEnterUser();");
            sb.AppendLine("    }");
            
            sb.AppendLine("");
            sb.AppendLine("    partial void OnEnterUser();");
            sb.AppendLine("    public override void OnUpdate() { OnUpdateUser(); }");
            sb.AppendLine("    partial void OnUpdateUser();");
            sb.AppendLine("}");
            sb.AppendLine("");
        }

        string fullPath = savePath + controllerName + "_Gen.cs";
        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>Spine FSM 生成成功: {fullPath}</color>");
    }
#endif
}