using System;
using UnityEngine;

[Serializable]
public class SoundItem
{
    public string soundName; // 音效唯一的Key，比如 "Jump", "Button_Click"
    public AudioClip clip;   // 音效文件
    [Range(0f, 1f)] 
    public float volumeScale = 1f; // 针对该素材的音量修正（有的素材原生声音太大了，可以在这里调小）
}