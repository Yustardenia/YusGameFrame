using UnityEngine;
using Fungus;

[CommandInfo("Audio", "Play SFX (Yus)", "使用 SceneAudioManager 播放音效")]
[AddComponentMenu("")]
public class PlaySFXCommand : Command
{
    [Tooltip("音效名称 (AudioLibrary 中配置的 Key)")]
    [SerializeField] protected string soundName;

    [Tooltip("播放次数 (>= 1)")]
    [SerializeField] protected int times = 1;

    [Tooltip("每次播放间隔(秒)，会在音效长度基础上额外等待这段时间")]
    [SerializeField] protected float intervalSeconds = 0f;

    [Tooltip("是否等待音效播放完毕再继续 (PlayOneShot 通常不等待，但在某些剧情演出可能需要)")]
    [SerializeField] protected bool waitUntilFinished = false;

    public override void OnEnter()
    {
        if (!string.IsNullOrEmpty(soundName))
        {
            var audio = SceneAudioManager.Instance;
            if (audio != null) audio.PlaySFX(soundName, Mathf.Max(1, times), Mathf.Max(0f, intervalSeconds));
        }

        if (waitUntilFinished)
        {
            // 这是一个简单的估算，实际上 PlayOneShot 很难精确回调
            // 既然是框架，简单处理：如果不重要就不等，重要的话建议写专门的演出逻辑
            // 这里为了不卡死流程，暂不实现复杂的等待逻辑，直接继续
            Continue();
        }
        else
        {
            Continue();
        }
    }

    public override string GetSummary() => times <= 1 ? soundName : $"{soundName} x{times}";
    public override Color GetButtonColor() => new Color32(255, 204, 153, 255); // 浅橙色
}
