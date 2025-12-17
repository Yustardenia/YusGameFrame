#if YUS_DOTWEEN
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 摇晃：像在说“不行不行”或“快看我”，来回摆动，循环 yoyo。
/// 用于提示错误、吸引注意力、做一点小情绪。
/// </summary>
public class YusUIWiggle : MonoBehaviour
{
    [Header("Wiggle")]
    [SerializeField] private float angle = 8f;
    [SerializeField] private float halfDuration = 0.08f;
    [SerializeField] private bool localRotate = true;

    [Header("Options")]
    [SerializeField] private bool unscaledTime = true;
    [SerializeField] private bool playOnEnable = false;

    private Quaternion _baseRotation;
    private Tween _tween;

    private void Awake()
    {
        _baseRotation = localRotate ? transform.localRotation : transform.rotation;
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play(int loops = 6)
    {
        Stop();

        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance.Tween : null;
        if (mgr != null && localRotate)
        {
            _tween = mgr.WiggleZ(transform, angle: angle, halfDuration: halfDuration, loops: loops, baseLocalRotation: _baseRotation, unscaledTime: unscaledTime, killTargetTweens: false, id: this);
            return;
        }

        float d = Mathf.Max(0.001f, halfDuration);
        Vector3 targetEuler = new Vector3(0f, 0f, angle);

        _tween = (localRotate
                ? transform.DOLocalRotate(targetEuler, d).SetRelative()
                : transform.DORotate(targetEuler, d).SetRelative())
            .SetEase(Ease.InOutSine)
            .SetLoops(loops, LoopType.Yoyo)
            .SetUpdate(unscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .SetId(this)
            .OnKill(ResetRotation);
    }

    public void Stop()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }
        ResetRotation();
    }

    private void ResetRotation()
    {
        if (localRotate) transform.localRotation = _baseRotation;
        else transform.rotation = _baseRotation;
    }
}

#endif
