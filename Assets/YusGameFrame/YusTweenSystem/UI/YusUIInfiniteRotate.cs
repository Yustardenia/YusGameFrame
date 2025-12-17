#if YUS_DOTWEEN
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 无限自转：像加载图标一样一直转，一刻不停地告诉玩家“我在努力工作”。
/// </summary>
public class YusUIInfiniteRotate : MonoBehaviour
{
    [Header("Rotate")]
    [SerializeField] private Vector3 axis = Vector3.forward;
    [SerializeField] private float degreesPerSecond = 360f;
    [SerializeField] private bool localRotate = true;

    [Header("Options")]
    [SerializeField] private bool unscaledTime = true;
    [SerializeField] private bool killTargetTweens = true;

    private Tween _tween;

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        Stop();

        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance.Tween : null;
        if (mgr != null)
        {
            _tween = mgr.InfiniteRotate(transform, axis, degreesPerSecond, localRotate, unscaledTime: unscaledTime, killTargetTweens: killTargetTweens, id: this);
            return;
        }

        if (killTargetTweens) transform.DOKill();

        float duration = Mathf.Approximately(degreesPerSecond, 0f) ? 0.001f : Mathf.Abs(360f / degreesPerSecond);
        Vector3 oneTurn = axis.normalized * 360f;

        _tween = (localRotate
                ? transform.DOLocalRotate(oneTurn, duration, RotateMode.FastBeyond360)
                : transform.DORotate(oneTurn, duration, RotateMode.FastBeyond360))
            .SetEase(YusEase.Average)
            .SetLoops(-1, LoopType.Incremental)
            .SetUpdate(unscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .SetId(this);
    }

    public void Stop()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }
    }
}

#endif
