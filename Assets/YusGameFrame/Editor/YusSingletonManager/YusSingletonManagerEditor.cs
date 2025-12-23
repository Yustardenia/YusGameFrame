using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(YusSingletonManager))]
public class YusSingletonManagerEditor : Editor
{
    private YusSingletonManager manager;
    private bool showScanResult;
    private string scanMessage = string.Empty;

    private void OnEnable()
    {
        manager = (YusSingletonManager)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Singleton Scan Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan and setup all singletons", GUILayout.Height(30)))
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
        if (manager == null) return;

        Undo.RecordObject(manager.gameObject, "Setup Singletons");
        Undo.RecordObject(manager, "Setup Manager Fields");

        List<Type> singletonTypes = FindAllSingletonTypes();
        int addedCount = 0;
        int linkedCount = 0;

        foreach (Type type in singletonTypes)
        {
            Component component = manager.GetComponentInChildren(type, true);

            if (component == null)
            {
                var childGo = new GameObject(type.Name);
                Undo.RegisterCreatedObjectUndo(childGo, "Create Singleton Child");

                childGo.transform.SetParent(manager.transform, false);
                component = childGo.AddComponent(type);
                addedCount++;
            }

            if (TryLinkField(manager, component))
            {
                linkedCount++;
            }

            if (component is MonoBehaviour mb)
            {
                manager.RegisterSingleton(mb);
                if (!string.IsNullOrEmpty(type.AssemblyQualifiedName))
                {
                    manager.RegisterSingletonTypeName(type.AssemblyQualifiedName);
                }
            }
        }

        EditorUtility.SetDirty(manager);

        scanMessage = $"Done.\nAdded components: {addedCount}\nLinked fields: {linkedCount}";
        showScanResult = true;
    }

    private static List<Type> FindAllSingletonTypes()
    {
        var result = new List<Type>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.FullName.StartsWith("Unity") && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("mscorlib"));

        foreach (Assembly assembly in assemblies)
        {
            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                assemblyTypes = ex.Types;
            }
            catch
            {
                continue;
            }

            foreach (Type type in assemblyTypes)
            {
                if (type == null) continue;
                if (!type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsAbstract) continue;

                bool hasInstanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static) != null;
                bool hasInstanceField = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static) != null;
                if (!hasInstanceProperty && !hasInstanceField) continue;

                if (type == typeof(YusSingletonManager)) continue;
                if (type.Name == "YusCamera2DManager") continue;

                result.Add(type);
            }
        }

        return result;
    }

    private static bool TryLinkField(YusSingletonManager manager, Component component)
    {
        FieldInfo[] fields = typeof(YusSingletonManager).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            if (field.FieldType == component.GetType())
            {
                field.SetValue(manager, component);
                return true;
            }
        }
        return false;
    }
}
