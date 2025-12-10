using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[CustomEditor(typeof(YusSingletonManager))]
public class YusSingletonManagerEditor : Editor
{
    private YusSingletonManager manager;
    private bool showScanResult = false;
    private string scanMessage = "";

    private void OnEnable()
    {
        manager = (YusSingletonManager)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(20);
        EditorGUILayout.LabelField("🕵️‍♂️ 智能扫描工具", EditorStyles.boldLabel);

        if (GUILayout.Button("一键扫描并装配所有单例", GUILayout.Height(30)))
        {
            ScanAndSetup();
        }

        if (showScanResult)
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(scanMessage, MessageType.Info);
        }
    }

    private void ScanAndSetup()
    {
        Undo.RecordObject(manager.gameObject, "Setup Singletons");
        Undo.RecordObject(manager, "Setup Manager Fields");

        var singletonTypes = FindAllSingletonTypes();
        int addedCount = 0;
        int linkedCount = 0;

        foreach (var type in singletonTypes)
        {
            // 1. 检查是否已挂载
            Component comp = manager.GetComponentInChildren(type, true);
            
            if (comp == null)
            {
                // 如果没挂载，创建一个子物体挂载它（保持层级整洁）
                GameObject childGO = new GameObject(type.Name);
                childGO.transform.SetParent(manager.transform);
                childGO.transform.localPosition = Vector3.zero;
                
                comp = childGO.AddComponent(type);
                Undo.RegisterCreatedObjectUndo(childGO, "Create Singleton Child");
                addedCount++;
                Debug.Log($"[YusManager] 自动创建并挂载: {type.Name}");
            }

            // 2. 尝试关联到 Manager 的字段
            if (TryLinkField(manager, comp))
            {
                linkedCount++;
            }
            
            // 3. 注册到 otherSingletons 列表
            manager.RegisterSingleton(comp as MonoBehaviour);
        }

        // 标记已修改，触发保存
        EditorUtility.SetDirty(manager);
        
        scanMessage = $"扫描完成！\n新增挂载: {addedCount} 个\n关联字段: {linkedCount} 个\n\n现在所有单例都已归位到此物体下。";
        showScanResult = true;
    }

    /// <summary>
    /// 查找所有继承自 MonoBehaviour 且包含 Instance 静态属性的类
    /// </summary>
    private List<Type> FindAllSingletonTypes()
    {
        List<Type> types = new List<Type>();
        
        // 获取所有程序集（过滤掉 Unity 原生的，只看用户的）
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.FullName.StartsWith("Unity") && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("mscorlib"));

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsAbstract) continue;

                // 检查是否有 public static Instance 属性或字段
                var prop = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                var field = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static);

                if (prop != null || field != null)
                {
                    // 排除 YusSingletonManager 自己
                    if (type != typeof(YusSingletonManager))
                    {
                        types.Add(type);
                    }
                }
            }
        }
        return types;
    }

    /// <summary>
    /// 利用反射，将组件赋值给 Manager 中类型匹配的字段
    /// </summary>
    private bool TryLinkField(YusSingletonManager manager, Component comp)
    {
        var fields = typeof(YusSingletonManager).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.FieldType == comp.GetType())
            {
                field.SetValue(manager, comp);
                return true;
            }
        }
        return false;
    }
}
