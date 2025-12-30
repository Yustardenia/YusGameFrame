using System;

[Obsolete("Use FastForwardManager instead.")]
public class SkipManager : FastForwardManager
{
    public static new SkipManager Instance => FastForwardManager.Instance as SkipManager;

    public bool IsSkipping => IsFastForwarding;

    public event Action<bool> OnSkipStateChanged
    {
        add => OnFastForwardStateChanged += value;
        remove => OnFastForwardStateChanged -= value;
    }

    public void StartSkip() => StartFastForward();
    public void StopSkip() => StopFastForward();
    public void ToggleSkip() => ToggleFastForward();
}
