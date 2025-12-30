#if YUS_DOTWEEN
using System;
using DG.Tweening;
using UnityEngine;

public static class YusTweenUIExtensions
{
    /// <summary>
    /// UI 缩放弹出：从 0 长到目标大小，像“面板从空气里长出来”。
    /// 默认用 OutBack，带一点可爱的回弹。
    /// </summary>
    public static Tween YusScalePopOpen(
        this Transform target,
        float duration = 0.5f,
        Vector3? endScale = null,
        Ease ease = YusEase.PopOut,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;

        Vector3 to = endScale ?? Vector3.one;
        return YusTween.ScaleFromTo(
            target,
            from: Vector3.zero,
            to: to,
            duration: duration,
            ease: ease,
            unscaledTime: unscaledTime,
            killTargetTweens: killTargetTweens,
            id: id,
            linkBehaviour: linkBehaviour);
    }

    /// <summary>
    /// UI 缩放消失：缩到 0 再执行回调，像“轻轻把面板按回口袋里”。
    /// 用 InBack 会有一点向内回弹的“收尾感”。
    /// </summary>
    public static Tween YusScalePopClose(
        this Transform target,
        float duration = 0.3f,
        Vector3? endScale = null,
        Ease ease = YusEase.PopIn,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        Action onComplete = null,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;

        Vector3 to = endScale ?? Vector3.zero;
        Tween tween = YusTween.ScaleTo(
            target,
            to: to,
            duration: duration,
            ease: ease,
            unscaledTime: unscaledTime,
            killTargetTweens: killTargetTweens,
            id: id,
            linkBehaviour: linkBehaviour);

        if (tween != null && onComplete != null)
        {
            tween.OnComplete(() => onComplete());
        }

        return tween;
    }
}

#endif
