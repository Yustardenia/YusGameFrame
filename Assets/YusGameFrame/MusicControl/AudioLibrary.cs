using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/AudioLibrary", fileName = "NewAudioLibrary")]
public class AudioLibrary : ScriptableObject
{
    [Header("音效列表")]
    public List<SoundItem> sounds = new List<SoundItem>();

    // 运行时查找用的字典
    private Dictionary<string, SoundItem> soundDict;

    /// <summary>
    /// 初始化字典
    /// </summary>
    public void Initialize()
    {
        // 1. 创建字典（每次调用都重新 new，防止残留）
        soundDict = new Dictionary<string, SoundItem>();
        
        // 2. 防空保护：如果列表是空的，直接返回，避免 foreach 报错
        if (sounds == null || sounds.Count == 0) return;

        foreach (var item in sounds)
        {
            // 防空保护：防止列表里有空元素（比如你点了加号但没配置）
            if (item == null || item.clip == null) continue;

            // 优先用配置的名字，如果没有配置，就用文件名
            string keyName = !string.IsNullOrEmpty(item.soundName) ? item.soundName : item.clip.name;

            if (!soundDict.ContainsKey(keyName))
            {
                soundDict.Add(keyName, item);
            }
        }
    }

    /// <summary>
    /// 获取音效数据
    /// </summary>
    public SoundItem GetSound(string name)
    {
        // 【关键修复】不要判断 bool 变量，直接判断字典是不是空
        // 如果字典为空，或者 Unity 重载了脚本导致字典丢失，这里会重新初始化
        if (soundDict == null) 
        {
            Initialize();
        }

        // 双重保险：如果 Initialize 之后 soundDict 还是 null（极少见，除非内存溢出），再防一手
        if (soundDict == null) return null;

        if (soundDict.TryGetValue(name, out SoundItem item))
        {
            return item;
        }
        
        // 调试建议：如果找不到，可以在这里打个Log，看看传进来的 name 是什么
        // Debug.LogWarning($"AudioLibrary: 找不到 Key 为 '{name}' 的音效");
        
        return null;
    }
}