using UnityEngine;

/// <summary>
/// 音频数据层：只负责存取数据和发通知，不负责播放
/// </summary>
public static class AudioData
{
    // 存档 Key (对应 SimpleSingleValueSaver 的文件名)
    private const string KEY_MUSIC = "Setting_MusicVol";
    private const string KEY_SFX = "Setting_SFXVol";

    public static float MusicVolume { get; private set; }
    public static float SFXVolume { get; private set; }

    // 静态构造函数：游戏第一次用到这个类时自动加载
    static AudioData()
    {
        // 1. 使用 YusSimple 读取二进制存档 (默认 1.0)
        MusicVolume = SimpleSingleValueSaver.Load(KEY_MUSIC, 1.0f);
        SFXVolume = SimpleSingleValueSaver.Load(KEY_SFX, 1.0f);
    }

    public static void SetMusicVolume(float value)
    {
        float newVal = Mathf.Clamp01(value);
        if (Mathf.Approximately(MusicVolume, newVal)) return; // 值没变就不折腾

        MusicVolume = newVal;

        // 2. 使用 YusSimple 保存到二进制
        SimpleSingleValueSaver.Save(KEY_MUSIC, MusicVolume);
        
        // 3. 使用 YusEvent 广播事件
        YusEventManager.Instance.Broadcast(YusEvents.OnMusicVolChange, MusicVolume);
    }

    public static void SetSFXVolume(float value)
    {
        float newVal = Mathf.Clamp01(value);
        if (Mathf.Approximately(SFXVolume, newVal)) return;

        SFXVolume = newVal;

        // 保存
        SimpleSingleValueSaver.Save(KEY_SFX, SFXVolume);
        
        // 广播
        YusEventManager.Instance.Broadcast(YusEvents.OnSFXVolChange, SFXVolume);
    }
}