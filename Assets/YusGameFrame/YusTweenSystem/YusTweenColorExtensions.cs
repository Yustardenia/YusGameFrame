#if YUS_DOTWEEN
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class YusTweenColorExtensions
{
    /// <summary>
    /// 颜色切换（迅速）：立刻换色，像“啪”地换了一张皮肤。
    /// </summary>
    public static void YusColorInstant(this Graphic graphic, Color to)
    {
        if (graphic == null) return;
        graphic.color = to;
    }

    /// <summary>
    /// 颜色切换（渐变）：从当前颜色慢慢染到目标颜色，像“颜料渗进去”。
    /// </summary>
    public static Tween YusColorFade(this Graphic graphic, Color to, float duration, Ease ease = YusEase.FastToSlow, bool unscaledTime = true, bool killTargetTweens = true, object id = null)
    {
        if (graphic == null) return null;
        if (killTargetTweens) YusTween.KillTargetTweens(graphic);

        Tween tween = graphic.DOColor(to, duration).SetEase(ease);
        return YusTween.ApplyDefaults(tween, graphic.gameObject, unscaledTime, id, LinkBehaviour.KillOnDestroy);
    }

    public static void YusColorInstant(this SpriteRenderer renderer, Color to)
    {
        if (renderer == null) return;
        renderer.color = to;
    }

    public static Tween YusColorFade(this SpriteRenderer renderer, Color to, float duration, Ease ease = YusEase.FastToSlow, bool unscaledTime = false, bool killTargetTweens = true, object id = null)
    {
        if (renderer == null) return null;
        if (killTargetTweens) YusTween.KillTargetTweens(renderer);

        Tween tween = renderer.DOColor(to, duration).SetEase(ease);
        return YusTween.ApplyDefaults(tween, renderer.gameObject, unscaledTime, id, LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// Renderer 换色（迅速）：默认操作 instance material（renderer.material），避免改到 sharedMaterial 影响全体。
    /// </summary>
    public static void YusColorInstant(this Renderer renderer, Color to, bool useSharedMaterial = false)
    {
        if (renderer == null) return;
        Material m = useSharedMaterial ? renderer.sharedMaterial : renderer.material;
        if (m == null) return;
        if (m.HasProperty("_Color")) m.color = to;
    }

    /// <summary>
    /// Renderer 换色（渐变）：默认操作 instance material（renderer.material），适合角色闪白、受击变色、环境渐变。
    /// </summary>
    public static Tween YusColorFade(this Renderer renderer, Color to, float duration, Ease ease = YusEase.FastToSlow, bool unscaledTime = false, bool killTargetTweens = true, bool useSharedMaterial = false, object id = null)
    {
        if (renderer == null) return null;
        if (killTargetTweens) YusTween.KillTargetTweens(renderer);

        Material m = useSharedMaterial ? renderer.sharedMaterial : renderer.material;
        if (m == null) return null;
        if (!m.HasProperty("_Color")) return null;

        Tween tween = m.DOColor(to, duration).SetEase(ease);
        return YusTween.ApplyDefaults(tween, renderer.gameObject, unscaledTime, id, LinkBehaviour.KillOnDestroy);
    }
}

#endif
