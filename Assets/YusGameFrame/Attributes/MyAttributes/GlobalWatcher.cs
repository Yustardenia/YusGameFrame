using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class GlobalWatcher : MonoBehaviour
{
    // --- 自动化核心：游戏启动时自动创建自己 ---
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        var go = new GameObject("[GlobalWatcher]");
        go.AddComponent<GlobalWatcher>();
        DontDestroyOnLoad(go);
    }

    // 缓存需要监视的对象和字段
    private struct WatchEntry
    {
        public object Target;
        public MemberInfo Member;
        public string Label;
    }

    private List<WatchEntry> _entries = new List<WatchEntry>();
    private float _scanTimer = 0f;

    void Update()
    {
        // 懒人模式：每隔 1 秒重新扫描一次场景里的新物体
        // (这是为了自动化付出的微小性能代价，不用手动 Register)
        _scanTimer += Time.deltaTime;
        if (_scanTimer > 1.0f)
        {
            _scanTimer = 0;
            ScanScene();
        }
    }

    void ScanScene()
    {
        _entries.Clear();
        // 暴力扫描所有 MonoBehaviour (性能注意：物体特别多时可优化)
        var allScripts = FindObjectsOfType<MonoBehaviour>();

        foreach (var script in allScripts)
        {
            if (script == null) continue;
            var type = script.GetType();

            // 找字段
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = f.GetCustomAttribute<WatchAttribute>();
                if (attr != null)
                {
                    _entries.Add(new WatchEntry { Target = script, Member = f, Label = attr.Label ?? f.Name });
                }
            }
            // 找属性
            foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = p.GetCustomAttribute<WatchAttribute>();
                if (attr != null)
                {
                    _entries.Add(new WatchEntry { Target = script, Member = p, Label = attr.Label ?? p.Name });
                }
            }
        }
    }

    void OnGUI()
    {
        if (_entries.Count == 0) return;

        // 简单的绘制风格
        GUI.skin.label.fontSize = 14;
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.color = Color.green;

        float y = 10;
        foreach (var entry in _entries)
        {
            if (entry.Target == null || (entry.Target is Object obj && obj == null)) continue;

            object value = null;
            if (entry.Member is FieldInfo f) value = f.GetValue(entry.Target);
            else if (entry.Member is PropertyInfo p) value = p.GetValue(entry.Target);

            string targetName = (entry.Target as MonoBehaviour)?.name ?? "Unknown";
            string text = $"[{targetName}] {entry.Label}: {value}";

            GUI.Label(new Rect(10, y, 500, 25), text);
            y += 25;
        }
    }
}