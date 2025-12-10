#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class KeepValueProcessor
{
    static KeepValueProcessor()
    {
        // 注册 Unity 播放模式状态改变的事件
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // 1. 即将退出运行时：保存数据
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ProcessFields(SaveField);
        }
        // 2. 完全回到编辑模式时：加载数据
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            ProcessFields(LoadField);
        }
    }

    // 定义操作委托
    private delegate void FieldOperation(MonoBehaviour target, FieldInfo field, string key);

    private static void ProcessFields(FieldOperation operation)
    {
        // 查找场景中所有的 MonoBehaviour
        // 这是一个较重的操作，但仅在切换模式时触发一次，影响可忽略
        MonoBehaviour[] scripts = Object.FindObjectsOfType<MonoBehaviour>();

        foreach (var script in scripts)
        {
            if (script == null) continue;

            // 获取所有字段 (public, private, protected)
            var fields = script.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                // 检查是否有 [KeepValue] 标记
                if (System.Attribute.IsDefined(field, typeof(KeepValueAttribute)))
                {
                    // 使用 GlobalObjectId 确保在 PlayMode 切换前后能对应上同一个物体
                    string id = GlobalObjectId.GetGlobalObjectIdSlow(script).ToString();
                    string key = $"KeepValue_Store_{id}_{field.Name}";

                    operation(script, field, key);
                }
            }
        }
    }

    // --- 保存逻辑 ---
    private static void SaveField(MonoBehaviour target, FieldInfo field, string key)
    {
        try
        {
            object value = field.GetValue(target);
            if (value == null) return;

            string strValue;

            // 针对常用类型进行特定序列化，保证稳定性
            if (value is int) strValue = "INT|" + value;
            else if (value is float) strValue = "FLT|" + value;
            else if (value is bool) strValue = "BOL|" + value;
            else if (value is string) strValue = "STR|" + value;
            else if (value is Vector2 v2) strValue = $"V2|{v2.x},{v2.y}";
            else if (value is Vector3 v3) strValue = $"V3|{v3.x},{v3.y},{v3.z}";
            else if (value is Color c) strValue = $"COL|{c.r},{c.g},{c.b},{c.a}";
            else
            {
                // 其他复杂类型尝试用 JsonUtility
                strValue = "OBJ|" + JsonUtility.ToJson(value);
            }

            EditorPrefs.SetString(key, strValue);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[KeepValue] Save failed for {field.Name}: {e.Message}");
        }
    }

    // --- 加载逻辑 ---
    private static void LoadField(MonoBehaviour target, FieldInfo field, string key)
    {
        if (!EditorPrefs.HasKey(key)) return;

        try
        {
            string raw = EditorPrefs.GetString(key);
            string typeTag = raw.Substring(0, 3); // 前3位是类型标记
            string data = raw.Substring(4);       // 第5位开始是数据
            object result = null;

            // 解析数据
            if (typeTag == "INT") result = int.Parse(data);
            else if (typeTag == "FLT") result = float.Parse(data);
            else if (typeTag == "BOL") result = bool.Parse(data);
            else if (typeTag == "STR") result = data;
            else if (typeTag == "V2")
            {
                var p = data.Split(',');
                result = new Vector2(float.Parse(p[0]), float.Parse(p[1]));
            }
            else if (typeTag == "V3")
            {
                var p = data.Split(',');
                result = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }
            else if (typeTag == "COL")
            {
                var p = data.Split(',');
                result = new Color(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));
            }
            else if (typeTag == "OBJ")
            {
                result = JsonUtility.FromJson(data, field.FieldType);
            }

            // 赋值并标记脏数据
            if (result != null)
            {
                field.SetValue(target, result);

                // 关键步骤：标记对象已修改，以便 Unity 识别变更
                EditorUtility.SetDirty(target);
                
                // 标记场景未保存 (出现星号)
                if (!EditorUtility.IsPersistent(target))
                {
                    EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
                }

                Debug.Log($"<color=#4EC9B0>[KeepValue]</color> 已保留变量: <b>{target.name}.{field.Name}</b> = {result}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[KeepValue] Load failed for {field.Name}: {e.Message}");
        }
        finally
        {
            // 清理缓存，防止 key 堆积
            EditorPrefs.DeleteKey(key);
        }
    }
}
#endif