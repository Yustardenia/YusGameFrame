using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AutoInjector
{
    // =========================================================
    // 运行时逻辑 (Runtime) - 解决 "报空引用" 的关键
    // =========================================================
    
    // [关键点] 在场景加载完、Awake执行前/后，自动执行一次注入
    // 这样即使变量没有 [SerializeField]，也会在游戏开始瞬间被重新赋值
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeInject()
    {
        InjectAll("Runtime");
    }

    // =========================================================
    // 编辑器逻辑 (Editor) - 解决 "Inspector 看不到" 的问题
    // =========================================================
#if UNITY_EDITOR
    [InitializeOnLoadMethod] // 编辑器重新编译代码后自动运行
    private static void EditorAutoInject()
    {
        // 监听进入 Play 模式的事件
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // 当我们按下 Play 按钮，但在游戏逻辑开始前，尝试注入一次并保存
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            InjectAll("Editor");
        }
    }
#endif

    // =========================================================
    // 核心注入逻辑 (通用)
    // =========================================================
    private static void InjectAll(string source)
    {
        // 暴力查找场景所有 MonoBehaviour (性能完全OK，因为只在初始化执行一次)
        var scripts = Object.FindObjectsOfType<MonoBehaviour>();

        foreach (var script in scripts)
        {
            if (script == null) continue;
            var type = script.GetType();
            
            // 查找所有 private 和 public 字段
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<GetAttribute>();
                if (attr == null) continue;

                // 1. 如果字段已经有值了，跳过 (尊重手动拖拽)
                // 注意：在 Runtime 阶段，如果因为 Domain Reload 变成了 null，这里刚好能检测到并重新赋值
                var currentVal = field.GetValue(script);
                if (currentVal is Object obj && obj != null) continue;

                // 2. 获取组件
                Component targetComp = null;
                if (attr.IncludeChildren)
                    targetComp = script.GetComponentInChildren(field.FieldType, true);
                else
                    targetComp = script.GetComponent(field.FieldType);

                // 3. 赋值
                if (targetComp != null)
                {
                    field.SetValue(script, targetComp);
                    
                    // 如果是编辑器模式，标记脏数据以便保存
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(script);
                    }
#endif
                }
                else
                {
                    Debug.LogError($"[AutoInjector] ⚠️ 在 {script.name} 上找不到 {field.FieldType.Name} ({source})", script);
                }
            }
        }
    }
}