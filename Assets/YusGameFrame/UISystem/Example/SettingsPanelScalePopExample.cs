#if YUS_DOTWEEN
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Example: scale-pop open/close for a settings panel.
/// Attach to a panel GameObject that also has a CanvasGroup (required by BasePanel).
/// </summary>
public class SettingsPanelScalePopExample : BasePanel
{
    public override void Open()
    {
        base.Open();

        if (YusSingletonManager.Instance != null && YusSingletonManager.Instance.Tween != null)
        {
            YusSingletonManager.Instance.Tween.ScalePopOpen(transform, duration: 0.5f, endScale: Vector3.one, id: this);
        }
        else
        {
            transform.YusScalePopOpen(
                duration: 0.5f,
                endScale: Vector3.one,
                ease: Ease.OutBack,
                unscaledTime: true,
                id: this);
        }
    }

    public override void Close()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (YusSingletonManager.Instance != null && YusSingletonManager.Instance.Tween != null)
        {
            YusSingletonManager.Instance.Tween.ScalePopClose(transform, base.Close, duration: 0.3f, endScale: Vector3.zero, id: this);
        }
        else
        {
            transform.YusScalePopClose(
                duration: 0.3f,
                endScale: Vector3.zero,
                ease: Ease.InBack,
                unscaledTime: true,
                id: this,
                onComplete: base.Close);
        }
    }
}

#endif
