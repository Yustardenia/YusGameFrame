#if YUS_DOTWEEN
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 颜色切换组件：提供“迅速换色”和“渐变染色”两种手感。
/// 适合按钮悬停高亮、选中态、受击闪烁、稀有度变色。
/// </summary>
public class YusUIColorSwitch : MonoBehaviour
{
    public enum TargetKind
    {
        Auto,
        Graphic,
        SpriteRenderer,
        Renderer
    }

    [Header("Target")]
    [SerializeField] private TargetKind targetKind = TargetKind.Auto;
    [SerializeField] private Graphic graphic;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Renderer rendererTarget;
    [SerializeField] private bool rendererUseSharedMaterial = false;

    [Header("Color")]
    [SerializeField] private Color onColor = Color.white;
    [SerializeField] private Color offColor = Color.gray;

    [Header("Tween")]
    [SerializeField] private float fadeDuration = 0.12f;
    [SerializeField] private Ease fadeEase = YusEase.FastToSlow;
    [SerializeField] private bool unscaledTime = true;
    [SerializeField] private bool killTargetTweens = true;

    private void Awake()
    {
        AutoBindIfNeeded();
    }

    private void AutoBindIfNeeded()
    {
        if (targetKind != TargetKind.Auto) return;

        if (graphic == null) graphic = GetComponent<Graphic>();
        if (graphic != null) return;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) return;

        if (rendererTarget == null) rendererTarget = GetComponent<Renderer>();
    }

    public void SetOnInstant() => SetInstant(onColor);
    public void SetOffInstant() => SetInstant(offColor);
    public void SetOnFade() => SetFade(onColor);
    public void SetOffFade() => SetFade(offColor);

    public void SetInstant(Color color)
    {
        AutoBindIfNeeded();

        if (graphic != null) graphic.YusColorInstant(color);
        else if (spriteRenderer != null) spriteRenderer.YusColorInstant(color);
        else if (rendererTarget != null) rendererTarget.YusColorInstant(color, rendererUseSharedMaterial);
    }

    public Tween SetFade(Color color)
    {
        AutoBindIfNeeded();

        if (graphic != null) return graphic.YusColorFade(color, fadeDuration, fadeEase, unscaledTime, killTargetTweens, id: this);
        if (spriteRenderer != null) return spriteRenderer.YusColorFade(color, fadeDuration, fadeEase, unscaledTime: unscaledTime, killTargetTweens: killTargetTweens, id: this);
        if (rendererTarget != null) return rendererTarget.YusColorFade(color, fadeDuration, fadeEase, unscaledTime: unscaledTime, killTargetTweens: killTargetTweens, useSharedMaterial: rendererUseSharedMaterial, id: this);
        return null;
    }
}

#endif
