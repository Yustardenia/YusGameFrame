using UnityEngine;
using Fungus;

[CommandInfo("Audio", "Play Music (Yus)", "使用 SceneAudioManager 播放背景音乐")]
[AddComponentMenu("")]
public class PlayMusicCommand : Command
{
    [Tooltip("音乐名称 (AudioLibrary 中配置的 Key)")]
    [SerializeField] protected string musicName;

    [Tooltip("是否是从头开始播放 (如果已经在播这首，是否重置)")]
    [SerializeField] protected bool restartIfSame = false;

    public override void OnEnter()
    {
        if (string.IsNullOrEmpty(musicName))
        {
            // 如果留空，且想要停止音乐，可以调 Stop
            // SceneAudioManager.Instance.StopMusic();
        }
        else
        {
            SceneAudioManager.Instance.PlayMusic(musicName);
        }
        
        Continue();
    }

    public override string GetSummary() => musicName;
    public override Color GetButtonColor() => new Color32(255, 153, 153, 255); // 浅粉色
}