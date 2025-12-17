#if YUS_DOTWEEN
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 翻牌效果：先转到 90 度“藏住脸”，中途切换正反面，再转回 0 度“亮相”。
/// 你会看到一个干净的“啪——翻过去”的感觉，而且不会把 Transform 永久转到 180 度。
/// </summary>
public class YusUIFlipCard : MonoBehaviour
{
    [Header("Face")]
    [SerializeField] private GameObject front;
    [SerializeField] private GameObject back;
    [SerializeField] private bool showFrontOnStart = true;

    [Header("Flip")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private Ease ease = Ease.InOutCubic;
    [SerializeField] private Vector3 axis = Vector3.up;

    [Header("Options")]
    [SerializeField] private bool unscaledTime = true;
    [SerializeField] private bool killTargetTweens = true;

    private bool _isFront;
    private Quaternion _baseRotation;
    private Tween _tween;

    private void Awake()
    {
        _baseRotation = transform.localRotation;
        _isFront = showFrontOnStart;
        ApplyFace();
    }

    private void OnEnable()
    {
        transform.localRotation = _baseRotation;
    }

    public void Flip()
    {
        _isFront = !_isFront;
        FlipTo(_isFront);
    }

    public void FlipToFront() => FlipTo(true);
    public void FlipToBack() => FlipTo(false);

    public void FlipTo(bool showFront)
    {
        _isFront = showFront;

        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance.Tween : null;
        if (mgr != null)
        {
            _tween = mgr.FlipCard(transform, front, back, _isFront, duration: duration, axis: axis, unscaledTime: unscaledTime, killTargetTweens: killTargetTweens, id: this);
            return;
        }

        if (killTargetTweens) transform.DOKill();
        if (_tween != null) _tween.Kill();

        Vector3 normalizedAxis = axis.sqrMagnitude < 0.0001f ? Vector3.up : axis.normalized;
        Quaternion halfRot = _baseRotation * Quaternion.AngleAxis(90f, normalizedAxis);

        float half = Mathf.Max(0.001f, duration * 0.5f);

        _tween = YusTween.NewSequence(unscaledTime: unscaledTime, id: this, linkGameObject: gameObject)
            .Append(transform.DOLocalRotateQuaternion(halfRot, half).SetEase(ease))
            .AppendCallback(ApplyFace)
            .Append(transform.DOLocalRotateQuaternion(_baseRotation, half).SetEase(ease));
    }

    private void ApplyFace()
    {
        if (front != null) front.SetActive(_isFront);
        if (back != null) back.SetActive(!_isFront);
    }
}

#endif
