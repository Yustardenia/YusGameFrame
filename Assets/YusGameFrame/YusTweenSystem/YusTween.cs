#if YUS_DOTWEEN
using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// A small DOTween wrapper that standardizes common options (unscaled time, kill-existing, linking).
/// Keep APIs simple and readable for gameplay/UI code.
/// </summary>
public static class YusTween
{
    /// <summary>
    /// 给 Tween “穿上统一制服”：是否走真实时间、是否绑定对象销毁、是否打上 id 标签。
    /// 这样业务层就不用每次都想“我是不是忘了 SetUpdate / SetLink / SetId？”。
    /// </summary>
    private static Tween ConfigureInternal(
        Tween tween,
        GameObject linkGameObject,
        bool unscaledTime,
        object id,
        LinkBehaviour linkBehaviour)
    {
        if (tween == null) return null;

        tween.SetUpdate(unscaledTime);
        if (id != null) tween.SetId(id);
        if (linkGameObject != null) tween.SetLink(linkGameObject, linkBehaviour);
        return tween;
    }

    /// <summary>
    /// Apply common YusTween defaults onto an existing DOTween Tween/Sequence.
    /// Use this when you need to build tweens directly via DOTween APIs (e.g. special cases),
    /// but still want consistent SetUpdate/SetLink/SetId behavior.
    /// </summary>
    public static Tween ApplyDefaults(
        Tween tween,
        GameObject linkGameObject,
        bool unscaledTime,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        return ConfigureInternal(tween, linkGameObject, unscaledTime, id, linkBehaviour);
    }

    public static Tween ScaleFromTo(
        Transform target,
        Vector3 from,
        Vector3 to,
        float duration,
        Ease ease = Ease.OutQuad,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;

        if (killTargetTweens) target.DOKill();

        target.localScale = from;
        Tween tween = target.DOScale(to, duration).SetEase(ease);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// 缩放到指定大小：就像把 UI/物体“捏”到目标比例。
    /// </summary>
    public static Tween ScaleTo(
        Transform target,
        Vector3 to,
        float duration,
        Ease ease = Ease.OutQuad,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;

        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOScale(to, duration).SetEase(ease);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// 移动到世界坐标：像“把物体拖到某个点”，适合角色、道具、世界 UI。
    /// </summary>
    public static Tween MoveTo(
        Transform target,
        Vector3 to,
        float duration,
        Ease ease = Ease.OutQuad,
        bool unscaledTime = false,
        bool killTargetTweens = true,
        bool snapping = false,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOMove(to, duration, snapping).SetEase(ease);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// 移动到本地坐标：像“在父物体的怀里挪位置”，适合 UI、挂点、局部动画。
    /// </summary>
    public static Tween MoveLocalTo(
        Transform target,
        Vector3 to,
        float duration,
        Ease ease = Ease.OutQuad,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        bool snapping = false,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOLocalMove(to, duration, snapping).SetEase(ease);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// 旋转到世界角度：像“把物体拧到某个方向”，用于角色转身、朝向变化。
    /// </summary>
    public static Tween RotateTo(
        Transform target,
        Vector3 toEulerAngles,
        float duration,
        Ease ease = Ease.OutQuad,
        RotateMode rotateMode = RotateMode.Fast,
        bool unscaledTime = false,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DORotate(toEulerAngles, duration, rotateMode).SetEase(ease);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// 旋转到本地角度：像“在父节点坐标系里拧一下”，适合 UI/子节点摆动。
    /// </summary>
    public static Tween RotateLocalTo(
        Transform target,
        Vector3 toLocalEulerAngles,
        float duration,
        Ease ease = Ease.OutQuad,
        RotateMode rotateMode = RotateMode.Fast,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOLocalRotate(toLocalEulerAngles, duration, rotateMode).SetEase(ease);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// “轻轻揍它一拳”：位置 Punch，会先冲出去一点再弹回来，适合按钮反馈/受击抖一下。
    /// </summary>
    public static Tween PunchPosition(
        Transform target,
        Vector3 punch,
        float duration,
        int vibrato = 10,
        float elasticity = 1f,
        bool snapping = false,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOPunchPosition(punch, duration, vibrato, elasticity, snapping);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// “捏一下再放开”：缩放 Punch，适合按钮按下、奖励弹一下、强调某个 UI。
    /// </summary>
    public static Tween PunchScale(
        Transform target,
        Vector3 punch,
        float duration,
        int vibrato = 10,
        float elasticity = 1f,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOPunchScale(punch, duration, vibrato, elasticity);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// “被震到晕头转向”：旋转 Punch，适合受击、失败提示、强调摇头。
    /// </summary>
    public static Tween PunchRotation(
        Transform target,
        Vector3 punch,
        float duration,
        int vibrato = 10,
        float elasticity = 1f,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOPunchRotation(punch, duration, vibrato, elasticity);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// “地震啦”：位置 Shake，会在范围内随机抖动，常用于相机/UI 抖动。
    /// </summary>
    public static Tween ShakePosition(
        Transform target,
        float duration,
        Vector3 strength,
        int vibrato = 10,
        float randomness = 90f,
        bool snapping = false,
        bool fadeOut = true,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOShakePosition(duration, strength, vibrato, randomness, snapping, fadeOut);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// “摇头拒绝”：旋转 Shake，适合弹窗提示、输入错误、失败提示。
    /// </summary>
    public static Tween ShakeRotation(
        Transform target,
        float duration,
        Vector3 strength,
        int vibrato = 10,
        float randomness = 90f,
        bool fadeOut = true,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOShakeRotation(duration, strength, vibrato, randomness, fadeOut);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// “抖成果冻”：缩放 Shake，适合强调/惊吓效果，让 UI 像果冻一样颤一下。
    /// </summary>
    public static Tween ShakeScale(
        Transform target,
        float duration,
        Vector3 strength,
        int vibrato = 10,
        float randomness = 90f,
        bool fadeOut = true,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (target == null) return null;
        if (killTargetTweens) target.DOKill();

        Tween tween = target.DOShakeScale(duration, strength, vibrato, randomness, fadeOut);
        return ConfigureInternal(tween, target.gameObject, unscaledTime, id, linkBehaviour);
    }

    /// <summary>
    /// 让 UI 渐隐/渐显：像给画面加一层“透明度滤镜”。
    /// </summary>
    public static Tween FadeTo(
        CanvasGroup canvasGroup,
        float to,
        float duration,
        Ease ease = Ease.OutQuad,
        bool unscaledTime = true,
        bool killTargetTweens = true,
        object id = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        if (canvasGroup == null) return null;

        if (killTargetTweens) canvasGroup.DOKill();

        Tween tween = canvasGroup.DOFade(to, duration).SetEase(ease);
        return ConfigureInternal(tween, canvasGroup.gameObject, unscaledTime, id, linkBehaviour);
    }

    public static Sequence NewSequence(
        bool unscaledTime = true,
        object id = null,
        GameObject linkGameObject = null,
        LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy)
    {
        Sequence seq = DOTween.Sequence();
        return (Sequence)ConfigureInternal(seq, linkGameObject, unscaledTime, id, linkBehaviour);
    }

    public static int KillId(object id, bool complete = false)
    {
        if (id == null) return 0;
        return DOTween.Kill(id, complete);
    }

    public static void KillTargetTweens(Component target, bool complete = false)
    {
        if (target == null) return;
        target.DOKill(complete);
    }
}

#endif
