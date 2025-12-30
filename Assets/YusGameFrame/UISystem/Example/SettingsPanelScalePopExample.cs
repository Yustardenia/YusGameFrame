#if YUS_DOTWEEN
using DG.Tweening;
using System;
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
        PlayOpenTween(null);
    }

    public override void OpenWithTransition(Action onComplete = null)
    {
        base.Open();
        PlayOpenTween(onComplete);
    }

    private void PlayOpenTween(Action onComplete)
    {
        Tween tween;
        if (YusSingletonManager.Instance != null && YusSingletonManager.Instance.Tween != null)
        {
            tween = YusSingletonManager.Instance.Tween.ScalePopOpen(transform, duration: 0.5f, endScale: Vector3.one, id: this);
        }
        else
        {
            tween = transform.YusScalePopOpen(
                duration: 0.5f,
                endScale: Vector3.one,
                ease: Ease.OutBack,
                unscaledTime: true,
                id: this);
        }

        if (tween != null && onComplete != null)
        {
            tween.OnComplete(() => onComplete());
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    public override void Close()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        PlayCloseTween(null);
    }

    public override void CloseWithTransition(Action onComplete = null)
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        PlayCloseTween(onComplete);
    }

    private void PlayCloseTween(Action onComplete)
    {
        if (YusSingletonManager.Instance != null && YusSingletonManager.Instance.Tween != null)
        {
            YusSingletonManager.Instance.Tween.ScalePopClose(
                transform,
                () =>
                {
                    base.Close();
                    onComplete?.Invoke();
                },
                duration: 0.3f,
                endScale: Vector3.zero,
                id: this);
        }
        else
        {
            transform.YusScalePopClose(
                duration: 0.3f,
                endScale: Vector3.zero,
                ease: Ease.InBack,
                unscaledTime: true,
                id: this,
                onComplete: () =>
                {
                    base.Close();
                    onComplete?.Invoke();
                });
        }
    }
}

#endif

