using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class YusAnimFSMGenerator : EditorWindow
{
    private AnimatorController animController;
    private string className = "Player";
    private string savePath = "Assets/YusGameFrame/AnimSystem/Anims/";

    private int layerIndex;
    private bool includeSubStateMachines = true;
    private float defaultCrossFadeDuration = 0.1f;

    [MenuItem(YusGameFrameEditorMenu.Root + "Generators/Anim/动画状态机生成器 (Anim To FSM)")]
    public static void ShowWindow()
    {
        GetWindow<YusAnimFSMGenerator>("Anim To FSM");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animator → FSM 代码生成器（旧版输出风格）", EditorStyles.boldLabel);

        animController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animController, typeof(AnimatorController), false);
        className = EditorGUILayout.TextField("生成类名前缀", className);

        using (new EditorGUI.DisabledScope(animController == null))
        {
            string[] layers = animController != null && animController.layers != null
                ? animController.layers.Select(l => l.name).ToArray()
                : new[] { "Base Layer" };
            if (layers.Length == 0) layers = new[] { "Base Layer" };
            layerIndex = Mathf.Clamp(layerIndex, 0, layers.Length - 1);
            layerIndex = EditorGUILayout.Popup("Layer", layerIndex, layers);
        }

        if (GUILayout.Button("选择保存路径"))
        {
            string path = EditorUtility.OpenFolderPanel("选择保存文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                savePath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
            }
        }
        GUILayout.Label($"保存路径: {savePath}");

        EditorGUILayout.Space();
        GUILayout.Label("生成选项", EditorStyles.boldLabel);
        includeSubStateMachines = EditorGUILayout.ToggleLeft("包含子状态机 (Sub-StateMachine)", includeSubStateMachines);
        defaultCrossFadeDuration = EditorGUILayout.Slider("Default CrossFade Duration", defaultCrossFadeDuration, 0f, 1f);

        EditorGUILayout.Space();
        if (GUILayout.Button("生成代码（Controller + States + Hashes）", GUILayout.Height(40)))
        {
            if (animController == null)
            {
                ShowNotification(new GUIContent("请先选择 AnimatorController"));
                return;
            }
            Generate();
        }
    }

    private void Generate()
    {
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
        layerIndex = Mathf.Clamp(layerIndex, 0, animController.layers.Length - 1);

        AnimatorControllerLayer layer = animController.layers[layerIndex];
        string layerPrefix = GetLayerPrefix(layer);

        var collected = new List<CollectedState>();
        CollectStates(layer.stateMachine, layerPrefix, collected, includeSubStateMachines);

        var legacyStates = BuildLegacyNaming(collected);
        GenerateHashesScript(className, legacyStates);
        GenerateControllerAndStatesScript(className, legacyStates, defaultCrossFadeDuration);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("成功", $"生成完成！\nCode: {savePath}", "OK");
    }

    private static string GetLayerPrefix(AnimatorControllerLayer layer)
    {
        return string.IsNullOrEmpty(layer.name) ? "Base_Layer" : layer.name.Replace(" ", "_");
    }

    private struct CollectedState
    {
        public AnimatorState State;
        public string Path;
        public int Hash;
    }

    private sealed class LegacyStateInfo
    {
        public string Path;
        public string LeafName;
        public string Suffix;
        public int Hash;
    }

    private static void CollectStates(AnimatorStateMachine sm, string pathPrefix, List<CollectedState> output, bool recurse)
    {
        if (sm == null) return;

        foreach (var child in sm.states)
        {
            var state = child.state;
            if (state == null) continue;
            string path = string.IsNullOrEmpty(pathPrefix) ? state.name : $"{pathPrefix}/{state.name}";
            output.Add(new CollectedState
            {
                State = state,
                Path = path,
                Hash = Animator.StringToHash(state.name)
            });
        }

        if (!recurse) return;

        foreach (var childSm in sm.stateMachines)
        {
            if (childSm.stateMachine == null) continue;
            string childPrefix = string.IsNullOrEmpty(pathPrefix) ? childSm.stateMachine.name : $"{pathPrefix}/{childSm.stateMachine.name}";
            CollectStates(childSm.stateMachine, childPrefix, output, true);
        }
    }

    private static List<LegacyStateInfo> BuildLegacyNaming(List<CollectedState> states)
    {
        var list = new List<LegacyStateInfo>();
        if (states == null) return list;

        foreach (var s in states.Where(s => s.State != null && !string.IsNullOrEmpty(s.Path)).OrderBy(s => s.Path))
        {
            list.Add(new LegacyStateInfo
            {
                Path = s.Path,
                LeafName = s.State.name,
                Hash = s.Hash
            });
        }

        var used = new HashSet<string>();

        foreach (var group in list.GroupBy(x => x.LeafName))
        {
            if (group.Count() == 1)
            {
                var one = group.First();
                one.Suffix = MakeUniqueIdentifier(ToIdentifier(one.LeafName), used);
                continue;
            }

            var items = group.OrderBy(x => x.Path).ToList();
            var segByItem = items.ToDictionary(i => i, i => i.Path.Split('/'));

            int take = 2;
            while (true)
            {
                var candidateByItem = new Dictionary<LegacyStateInfo, string>();
                foreach (var item in items)
                {
                    var seg = segByItem[item];
                    int start = Mathf.Max(0, seg.Length - take);
                    candidateByItem[item] = ToIdentifier(string.Join("_", seg.Skip(start)));
                }

                if (candidateByItem.Values.Distinct().Count() == candidateByItem.Count)
                {
                    foreach (var kv in candidateByItem)
                        kv.Key.Suffix = MakeUniqueIdentifier(kv.Value, used);
                    break;
                }

                take++;
                if (take > items.Max(i => segByItem[i].Length))
                {
                    foreach (var item in items)
                    {
                        string fallback = ToIdentifier(item.Path.Replace("/", "_"));
                        item.Suffix = MakeUniqueIdentifier(fallback, used);
                    }
                    break;
                }
            }
        }

        return list.OrderBy(x => x.Path).ToList();
    }

    private void GenerateHashesScript(string prefix, List<LegacyStateInfo> states)
    {
        string hashClass = prefix + "AnimHash";
        var sb = new StringBuilder();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Generated by YusAnimFSMGenerator (legacy output).");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("");
        sb.AppendLine($"public static class {hashClass}");
        sb.AppendLine("{");
        sb.AppendLine("    // Animator state hashes (by state name; generated from AnimatorController).");
        foreach (var s in states)
        {
            sb.AppendLine($"    // {s.Path}");
            sb.AppendLine($"    public const int {s.Suffix} = {s.Hash};");
            sb.AppendLine("");
        }
        sb.AppendLine("}");

        File.WriteAllText(savePath + hashClass + "_Gen.cs", sb.ToString(), Encoding.UTF8);
    }

    private void GenerateControllerAndStatesScript(string prefix, List<LegacyStateInfo> states, float crossFadeDuration)
    {
        string controllerName = prefix + "Controller";
        string hashClass = prefix + "AnimHash";

        var sb = new StringBuilder();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Generated by YusAnimFSMGenerator (legacy output).");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("");

        sb.AppendLine("[RequireComponent(typeof(Animator))]");
        sb.AppendLine($"public partial class {controllerName} : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine($"    public YusFSM<{controllerName}> fsm;");
        sb.AppendLine("    public Animator Animator { get; private set; }");
        sb.AppendLine("");
        sb.AppendLine("    [Header(\"AnimSystem\")]");
        sb.AppendLine($"    [SerializeField] private float defaultCrossFadeDuration = {crossFadeDuration.ToString("0.###", CultureInfo.InvariantCulture)}f;");
        sb.AppendLine("    public float DefaultCrossFadeDuration => defaultCrossFadeDuration;");
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
        sb.AppendLine("    partial void OnInit();");
        sb.AppendLine("}");
        sb.AppendLine("");

        foreach (var s in states)
        {
            string stateClassName = prefix + s.Suffix + "State";
            sb.AppendLine($"// State {s.Path}");
            sb.AppendLine($"public partial class {stateClassName} : YusState<{controllerName}>");
            sb.AppendLine("{");
            sb.AppendLine("    public override void OnEnter()");
            sb.AppendLine("    {");
            sb.AppendLine($"        owner.Animator.CrossFade({hashClass}.{s.Suffix}, owner.DefaultCrossFadeDuration);");
            sb.AppendLine("        OnEnterUser();");
            sb.AppendLine("    }");
            sb.AppendLine("");
            sb.AppendLine("    partial void OnEnterUser();");
            sb.AppendLine("    public override void OnUpdate() { OnUpdateUser(); }");
            sb.AppendLine("    partial void OnUpdateUser();");
            sb.AppendLine("}");
            sb.AppendLine("");
        }

        File.WriteAllText(savePath + controllerName + "_Gen.cs", sb.ToString(), Encoding.UTF8);
    }

    private static string ToIdentifier(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "_";
        var sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
            sb.Append(ok ? c : '_');
        }
        if (sb.Length == 0) sb.Append('_');
        if (sb[0] >= '0' && sb[0] <= '9') sb.Insert(0, '_');
        return sb.ToString();
    }

    private static string MakeUniqueIdentifier(string baseName, HashSet<string> used)
    {
        string name = string.IsNullOrEmpty(baseName) ? "_" : baseName;
        if (used.Add(name)) return name;
        int i = 2;
        while (true)
        {
            string candidate = $"{name}_{i}";
            if (used.Add(candidate)) return candidate;
            i++;
        }
    }
}
