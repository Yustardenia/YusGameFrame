using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using System;

public class YusFSMDebugger : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem(YusGameFrameEditorMenu.Root + "Systems/FSM/FSM 调试器 (State Machine)")]
    public static void ShowWindow()
    {
        GetWindow<YusFSMDebugger>("FSM 调试器");
    }

    private void OnInspectorUpdate()
    {
        // 保持界面实时刷新，否则 Update 里的状态变化看不出来
        Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Label("FSM 实时状态监控", EditorStyles.boldLabel);
        
        GameObject selectedGO = Selection.activeGameObject;

        if (selectedGO == null)
        {
            EditorGUILayout.HelpBox("请在场景中选中一个挂载了 FSM 的物体", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("当前选中:", selectedGO.name, EditorStyles.boldLabel);
        
        // 分割线
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2)); 

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // --- 核心魔法：反射扫描所有 MonoBehaviour ---
        MonoBehaviour[] scripts = selectedGO.GetComponents<MonoBehaviour>();
        bool foundFSM = false;

        foreach (var mono in scripts)
        {
            if (mono == null) continue;

            // 扫描这个脚本里的所有字段
            // BindingFlags: 搜索 公有/私有、实例字段
            FieldInfo[] fields = mono.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                // 检查字段类型是不是 YusFSM 的泛型
                // field.FieldType.Name 包含 "YusFSM" 且是泛型
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(YusFSM<>))
                {
                    foundFSM = true;
                    object fsmInstance = field.GetValue(mono);
                    DrawFSMInfo(mono.GetType().Name, fsmInstance);
                }
            }
        }

        if (!foundFSM)
        {
            EditorGUILayout.LabelField("在该物体上未找到 YusFSM 实例。");
        }

        EditorGUILayout.EndScrollView();
    }

    // 绘制单个 FSM 的信息
    private void DrawFSMInfo(string ownerScriptName, object fsmInstance)
    {
        if (fsmInstance == null)
        {
            EditorGUILayout.HelpBox($"脚本 {ownerScriptName} 有 FSM 字段，但尚未初始化 (为 null)", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        
        // 1. 标题
        GUI.color = new Color(0.7f, 0.8f, 1f);
        GUILayout.Label($"持有者: {ownerScriptName}", EditorStyles.boldLabel);
        GUI.color = Color.white;

        // 利用反射获取 YusFSM 的属性 (因为在这里我们不知道泛型 T 是什么，只能用反射)
        Type fsmType = fsmInstance.GetType();
        
        object currentStateObj = fsmType.GetProperty("CurrentState")?.GetValue(fsmInstance);
        object previousStateObj = fsmType.GetProperty("PreviousState")?.GetValue(fsmInstance);

        // 2. 当前状态 (高亮)
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("当前状态:", GUILayout.Width(80));
        string currentName = currentStateObj != null ? currentStateObj.GetType().Name : "None (未启动)";
        GUI.color = Color.green;
        GUILayout.Label(currentName, EditorStyles.boldLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        // 3. 上个状态
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("上个状态:", GUILayout.Width(80));
        string prevName = previousStateObj != null ? previousStateObj.GetType().Name : "None";
        GUI.color = Color.gray;
        GUILayout.Label(prevName);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        // 4. 状态缓存列表 (列出所有由 ChangeState 触发过的状态)
        GUILayout.Space(5);
        GUILayout.Label("已缓存的状态 (Active Cache):", EditorStyles.miniBoldLabel);
        
        // 调用 Debug_GetStateCache
        MethodInfo debugMethod = fsmType.GetMethod("Debug_GetStateCache");
        if (debugMethod != null)
        {
            var dict = debugMethod.Invoke(fsmInstance, null) as IDictionary;
            if (dict != null)
            {
                foreach (DictionaryEntry kvp in dict)
                {
                    Type stateType = (Type)kvp.Key;
                    bool isActive = (stateType.Name == currentName);

                    // 绘制一行状态
                    EditorGUILayout.BeginHorizontal("helpbox");
                    // 图标
                    GUILayout.Label(isActive ? ">" : " ", GUILayout.Width(20));
                    // 名字
                    GUI.color = isActive ? Color.green : Color.white;
                    GUILayout.Label(stateType.Name);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }
}
