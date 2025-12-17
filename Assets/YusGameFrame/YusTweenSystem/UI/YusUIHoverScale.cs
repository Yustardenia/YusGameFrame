#if YUS_DOTWEEN
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 鼠标悬停缩放：指针一靠近就“轻轻鼓起来”，离开就“乖乖缩回去”。
/// 适合按钮、卡片、列表项，让玩家感觉“这个东西可以点”。
/// </summary>
public class YusUIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverMultiplier = 1.05f;
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private Ease ease = YusEase.CleanStop;

    [Header("Options")]
    [SerializeField] private bool unscaledTime = true;
    [SerializeField] private bool killTargetTweens = true;

    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = _baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance.Tween : null;
        if (mgr != null)
        {
            mgr.HoverEnter(transform, hoverMultiplier: hoverMultiplier, duration: duration, baseScale: _baseScale, unscaledTime: unscaledTime, killTargetTweens: killTargetTweens, id: this);
            return;
        }

        Vector3 to = _baseScale * hoverMultiplier;
        YusTween.ScaleTo(transform, to, duration, ease, unscaledTime, killTargetTweens, id: this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance.Tween : null;
        if (mgr != null)
        {
            mgr.HoverExit(transform, duration: duration, baseScale: _baseScale, unscaledTime: unscaledTime, killTargetTweens: killTargetTweens, id: this);
            return;
        }

        YusTween.ScaleTo(transform, _baseScale, duration, ease, unscaledTime, killTargetTweens, id: this);
    }
}

#endif
