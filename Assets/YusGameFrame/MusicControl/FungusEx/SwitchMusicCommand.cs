using UnityEngine;
using Fungus;

[CommandInfo("Audio", "Switch/Return Music", "临时切换 BGM 或 恢复上一首 BGM")]
[AddComponentMenu("")]
public class SwitchMusicCommand : Command
{
    public enum Mode { Switch, Return }
    
    [SerializeField] protected Mode mode = Mode.Switch;
    
    [Tooltip("切换模式下：新音乐的名字")]
    [SerializeField] protected string musicName;

    public override void OnEnter()
    {
        if (mode == Mode.Switch)
        {
            SceneAudioManager.Instance.SwitchMusicTemporary(musicName);
        }
        else
        {
            SceneAudioManager.Instance.ReturnToPreviousMusic();
        }
        Continue();
    }

    public override string GetSummary() => mode == Mode.Switch ? $"Switch to {musicName}" : "Return to Previous";
}