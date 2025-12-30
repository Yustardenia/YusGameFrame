#if YUS_DOTWEEN
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 点击反馈：按下时“压一下”，松开时“弹回来”，可选再来一拳 Punch 作为回馈。
/// 让按钮从“图片”变成“有手感的东西”。
/// </summary>
public class YusUIClickFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Press Scale")]
    [SerializeField] private float pressMultiplier = 0.95f;
    [SerializeField] private float pressDuration = 0.06f;
    [SerializeField] private Ease pressEase = YusEase.CleanStop;

    [Header("Click Punch (Optional)")]
    [SerializeField] private bool punchOnClick = true;
    [SerializeField] private Vector3 punch = new Vector3(0.08f, 0.08f, 0f);
    [SerializeField] private float punchDuration = 0.18f;

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

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector3 to = _baseScale * pressMultiplier;
        YusTween.ScaleTo(transform, to, pressDuration, pressEase, unscaledTime, killTargetTweens, id: this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        YusTween.ScaleTo(transform, _baseScale, pressDuration, pressEase, unscaledTime, killTargetTweens, id: this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!punchOnClick) return;
        YusTween.PunchScale(transform, punch, punchDuration, unscaledTime: unscaledTime, killTargetTweens: killTargetTweens, id: this);
    }
}

#endif
