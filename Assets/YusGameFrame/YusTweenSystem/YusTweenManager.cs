#if YUS_DOTWEEN
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 动画总管（单例）：
/// 你可以把它理解成“动画指挥官”——所有 DOTween 的常用动作都从这里发号施令。
/// 好处是：项目里到处都用同一种节奏、同一种默认手感、同一种管理方式（Kill/Link/UnscaledTime）。
/// </summary>
public class YusTweenManager : MonoBehaviour
{
    public static YusTweenManager Instance { get; private set; }

    [Header("Defaults (默认手感)")]
    [SerializeField] private bool defaultUnscaledTimeForUI = true;
    [SerializeField] private bool defaultKillTargetTweens = true;

    private static bool _isQuitting;

    private readonly Dictionary<int, Vector3> _baseScaleByTransformId = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, Quaternion> _baseLocalRotationByTransformId = new Dictionary<int, Quaternion>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TryAttachToSingletonManagerOrPersist();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        _baseScaleByTransformId.Clear();
        _baseLocalRotationByTransformId.Clear();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void TryAttachToSingletonManagerOrPersist()
    {
        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance : FindObjectOfType<YusSingletonManager>();
        if (mgr != null)
        {
            if (transform.parent != mgr.transform)
            {
                transform.SetParent(mgr.transform, false);
            }

            if (mgr.Tween == null) mgr.Tween = this;
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private static bool EnsureInstance()
    {
        if (_isQuitting) return false;
        if (Instance != null) return true;

        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance : FindObjectOfType<YusSingletonManager>();
        if (mgr != null && mgr.Tween != null)
        {
            Instance = mgr.Tween;
            return Instance != null;
        }

        var found = FindObjectOfType<YusTweenManager>();
        if (found != null)
        {
            Instance = found;
            return true;
        }

        var go = new GameObject(nameof(YusTweenManager));
        Instance = go.AddComponent<YusTweenManager>();
        return Instance != null;
    }

    private bool ResolveUnscaledForUI(bool? unscaledTime) => unscaledTime ?? defaultUnscaledTimeForUI;
    private bool ResolveKill(bool? killTargetTweens) => killTargetTweens ?? defaultKillTargetTweens;

    private static object ResolveId(object id, UnityEngine.Object fallback) => id ?? (object)fallback ?? id;

    private Vector3 GetOrCaptureBaseScale(Transform t, Vector3? baseScale)
    {
        int key = t.GetInstanceID();
        if (baseScale.HasValue)
        {
            _baseScaleByTransformId[key] = baseScale.Value;
            return baseScale.Value;
        }

        if (_baseScaleByTransformId.TryGetValue(key, out var cached)) return cached;
        _baseScaleByTransformId[key] = t.localScale;
        return t.localScale;
    }

    private Quaternion GetOrCaptureBaseLocalRotation(Transform t, Quaternion? baseLocalRotation)
    {
        int key = t.GetInstanceID();
        if (baseLocalRotation.HasValue)
        {
            _baseLocalRotationByTransformId[key] = baseLocalRotation.Value;
            return baseLocalRotation.Value;
        }

        if (_baseLocalRotationByTransformId.TryGetValue(key, out var cached)) return cached;
        _baseLocalRotationByTransformId[key] = t.localRotation;
        return t.localRotation;
    }

    /// <summary>
    /// 立刻停掉一个目标身上的动画：像“导演喊停”。
    /// </summary>
    public void StopTarget(Component target, bool complete = false)
    {
        if (target == null) return;
        target.DOKill(complete);
    }

    /// <summary>
    /// 立刻停掉某个 id 的动画：像“一键清场”，常用于按钮反复点击时防抖。
    /// </summary>
    public int StopId(object id, bool complete = false)
    {
        if (id == null) return 0;
        return DOTween.Kill(id, complete);
    }

    /// <summary>
    /// 缩放弹出：从 0 长到目标大小，像“面板从空气里长出来”，自带一点回弹。
    /// </summary>
    public Tween ScalePopOpen(Transform target, float duration = 0.5f, Vector3? endScale = null, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (target == null) return null;
        return YusTween.ScaleFromTo(
            target,
            from: Vector3.zero,
            to: endScale ?? Vector3.one,
            duration: duration,
            ease: YusEase.PopOut,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 移动到世界坐标：像“把物体拖到某个点”，适合角色、道具、世界空间 UI。
    /// </summary>
    public Tween MoveTo(
        Transform target,
        Vector3 to,
        float duration,
        Ease ease = Ease.OutQuad,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        bool snapping = false,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.MoveTo(
            target,
            to,
            duration,
            ease,
            unscaledTime: unscaledTime ?? false,
            killTargetTweens: ResolveKill(killTargetTweens),
            snapping: snapping,
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 移动到本地坐标：在父节点坐标系里移动，适合挂点/局部动画。
    /// </summary>
    public Tween MoveLocalTo(
        Transform target,
        Vector3 to,
        float duration,
        Ease ease = Ease.OutQuad,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        bool snapping = false,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.MoveLocalTo(
            target,
            to,
            duration,
            ease,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            snapping: snapping,
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 旋转到世界角度：把物体“拧到某个方向”。
    /// </summary>
    public Tween RotateTo(
        Transform target,
        Vector3 toEulerAngles,
        float duration,
        Ease ease = Ease.OutQuad,
        RotateMode rotateMode = RotateMode.Fast,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.RotateTo(
            target,
            toEulerAngles,
            duration,
            ease,
            rotateMode,
            unscaledTime: unscaledTime ?? false,
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 旋转到本地角度：在父节点坐标系里“拧一下”。
    /// </summary>
    public Tween RotateLocalTo(
        Transform target,
        Vector3 toLocalEulerAngles,
        float duration,
        Ease ease = Ease.OutQuad,
        RotateMode rotateMode = RotateMode.Fast,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.RotateLocalTo(
            target,
            toLocalEulerAngles,
            duration,
            ease,
            rotateMode,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 缩放到指定大小：像“捏”到目标比例。
    /// </summary>
    public Tween ScaleTo(
        Transform target,
        Vector3 to,
        float duration,
        Ease ease = Ease.OutQuad,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.ScaleTo(
            target,
            to,
            duration,
            ease,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 位置 Punch：轻轻揍它一拳，会冲出去一点再弹回来。
    /// </summary>
    public Tween PunchPosition(
        Transform target,
        Vector3 punch,
        float duration,
        int vibrato = 10,
        float elasticity = 1f,
        bool snapping = false,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.PunchPosition(
            target,
            punch,
            duration,
            vibrato,
            elasticity,
            snapping,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target));
    }

    /// <summary>
    /// 缩放 Punch：捏一下再放开，按钮/奖励常用。
    /// </summary>
    public Tween PunchScale(
        Transform target,
        Vector3 punch,
        float duration,
        int vibrato = 10,
        float elasticity = 1f,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.PunchScale(
            target,
            punch,
            duration,
            vibrato,
            elasticity,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target));
    }

    /// <summary>
    /// 旋转 Punch：被震到晕头转向，适合摇头/受击。
    /// </summary>
    public Tween PunchRotation(
        Transform target,
        Vector3 punch,
        float duration,
        int vibrato = 10,
        float elasticity = 1f,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.PunchRotation(
            target,
            punch,
            duration,
            vibrato,
            elasticity,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target));
    }

    /// <summary>
    /// 位置 Shake：像地震一样抖一抖，适合相机/世界物体抖动。
    /// </summary>
    public Tween ShakePosition(
        Transform target,
        float duration,
        Vector3 strength,
        int vibrato = 10,
        float randomness = 90f,
        bool snapping = false,
        bool fadeOut = true,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.ShakePosition(
            target,
            duration,
            strength,
            vibrato,
            randomness,
            snapping,
            fadeOut,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target));
    }

    /// <summary>
    /// 旋转 Shake：摇头拒绝/提示错误的那种抖。
    /// </summary>
    public Tween ShakeRotation(
        Transform target,
        float duration,
        Vector3 strength,
        int vibrato = 10,
        float randomness = 90f,
        bool fadeOut = true,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.ShakeRotation(
            target,
            duration,
            strength,
            vibrato,
            randomness,
            fadeOut,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target));
    }

    /// <summary>
    /// 缩放 Shake：抖成果冻，适合强调/惊吓/故障感。
    /// </summary>
    public Tween ShakeScale(
        Transform target,
        float duration,
        Vector3 strength,
        int vibrato = 10,
        float randomness = 90f,
        bool fadeOut = true,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (target == null) return null;
        return YusTween.ShakeScale(
            target,
            duration,
            strength,
            vibrato,
            randomness,
            fadeOut,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target));
    }

    /// <summary>
    /// 缩放消失：缩到 0 后再执行回调，像“把面板按回口袋里”。
    /// </summary>
    public Tween ScalePopClose(Transform target, Action onComplete, float duration = 0.3f, Vector3? endScale = null, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (target == null) return null;
        Tween t = YusTween.ScaleTo(
            target,
            to: endScale ?? Vector3.zero,
            duration: duration,
            ease: YusEase.PopIn,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);

        if (t != null && onComplete != null) t.OnComplete(() => onComplete());
        return t;
    }

    /// <summary>
    /// 悬停开始：轻轻鼓起来，告诉玩家“我可以点我可以点”。
    /// </summary>
    public Tween HoverEnter(Transform target, float hoverMultiplier = 1.05f, float duration = 0.12f, Vector3? baseScale = null, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (target == null) return null;
        Vector3 baseS = GetOrCaptureBaseScale(target, baseScale);
        return YusTween.ScaleTo(
            target,
            to: baseS * hoverMultiplier,
            duration: duration,
            ease: YusEase.CleanStop,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 悬停结束：乖乖回到原来的大小，不抢戏。
    /// </summary>
    public Tween HoverExit(Transform target, float duration = 0.12f, Vector3? baseScale = null, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (target == null) return null;
        Vector3 baseS = GetOrCaptureBaseScale(target, baseScale);
        return YusTween.ScaleTo(
            target,
            to: baseS,
            duration: duration,
            ease: YusEase.CleanStop,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 按下：像把按钮按进去了，立刻有“手感”。
    /// </summary>
    public Tween PressDown(Transform target, float pressMultiplier = 0.95f, float duration = 0.06f, Vector3? baseScale = null, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (target == null) return null;
        Vector3 baseS = GetOrCaptureBaseScale(target, baseScale);
        return YusTween.ScaleTo(
            target,
            to: baseS * pressMultiplier,
            duration: duration,
            ease: YusEase.CleanStop,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 松开：回到原本大小，像“弹回”。
    /// </summary>
    public Tween PressUp(Transform target, float duration = 0.06f, Vector3? baseScale = null, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (target == null) return null;
        Vector3 baseS = GetOrCaptureBaseScale(target, baseScale);
        return YusTween.ScaleTo(
            target,
            to: baseS,
            duration: duration,
            ease: YusEase.CleanStop,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target),
            linkBehaviour: LinkBehaviour.KillOnDestroy);
    }

    /// <summary>
    /// 点击回馈：给按钮来一拳 Punch，让玩家觉得“我点到了，我成功了”。
    /// </summary>
    public Tween ClickPunch(Transform target, Vector3 punch, float duration = 0.18f, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (target == null) return null;
        return YusTween.PunchScale(
            target,
            punch: punch,
            duration: duration,
            unscaledTime: ResolveUnscaledForUI(unscaledTime),
            killTargetTweens: ResolveKill(killTargetTweens),
            id: ResolveId(id, target));
    }

    /// <summary>
    /// 无限自转：像加载图标一样一直转，直到你喊停。
    /// </summary>
    public Tween InfiniteRotate(Transform target, Vector3 axis, float degreesPerSecond = 360f, bool localRotate = true, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (target == null) return null;
        if (ResolveKill(killTargetTweens)) target.DOKill();

        float duration = Mathf.Approximately(degreesPerSecond, 0f) ? 0.001f : Mathf.Abs(360f / degreesPerSecond);
        Vector3 oneTurn = (axis.sqrMagnitude < 0.0001f ? Vector3.forward : axis.normalized) * 360f;

        Tween t = (localRotate
                ? target.DOLocalRotate(oneTurn, duration, RotateMode.FastBeyond360)
                : target.DORotate(oneTurn, duration, RotateMode.FastBeyond360))
            .SetEase(YusEase.Average)
            .SetLoops(-1, LoopType.Incremental);

        t.SetUpdate(ResolveUnscaledForUI(unscaledTime));
        t.SetLink(target.gameObject, LinkBehaviour.KillOnDestroy);
        t.SetId(ResolveId(id, target));
        return t;
    }

    /// <summary>
    /// 翻牌：先转到 90 度“藏住脸”，中途切换正反面，再转回去“亮相”。
    /// </summary>
    public Tween FlipCard(
        Transform card,
        GameObject front,
        GameObject back,
        bool showFrontAfterFlip,
        float duration = 0.35f,
        Vector3? axis = null,
        bool? unscaledTime = null,
        bool? killTargetTweens = null,
        object id = null)
    {
        if (card == null) return null;
        if (ResolveKill(killTargetTweens)) card.DOKill();

        Quaternion baseRot = GetOrCaptureBaseLocalRotation(card, null);
        Vector3 ax = axis.HasValue ? axis.Value : Vector3.up;
        if (ax.sqrMagnitude < 0.0001f) ax = Vector3.up;
        ax.Normalize();

        Quaternion halfRot = baseRot * Quaternion.AngleAxis(90f, ax);
        float half = Mathf.Max(0.001f, duration * 0.5f);

        void ApplyFace()
        {
            if (front != null) front.SetActive(showFrontAfterFlip);
            if (back != null) back.SetActive(!showFrontAfterFlip);
        }

        Sequence seq = YusTween.NewSequence(unscaledTime: ResolveUnscaledForUI(unscaledTime), id: ResolveId(id, card), linkGameObject: card.gameObject);
        seq.Append(card.DOLocalRotateQuaternion(halfRot, half).SetEase(YusEase.CleanStop));
        seq.AppendCallback(ApplyFace);
        seq.Append(card.DOLocalRotateQuaternion(baseRot, half).SetEase(YusEase.CleanStop));
        return seq;
    }

    /// <summary>
    /// 摇晃：像“摇头拒绝”或“提醒你看这里”，来回摆动一阵子。
    /// </summary>
    public Tween WiggleZ(Transform target, float angle = 8f, float halfDuration = 0.08f, int loops = 6, Quaternion? baseLocalRotation = null, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (target == null) return null;
        if (ResolveKill(killTargetTweens)) target.DOKill();

        Quaternion baseRot = GetOrCaptureBaseLocalRotation(target, baseLocalRotation);

        Tween t = target.DOLocalRotate(new Vector3(0f, 0f, angle), Mathf.Max(0.001f, halfDuration))
            .SetRelative()
            .SetEase(Ease.InOutSine)
            .SetLoops(loops, LoopType.Yoyo)
            .OnKill(() => target.localRotation = baseRot);

        t.SetUpdate(ResolveUnscaledForUI(unscaledTime));
        t.SetLink(target.gameObject, LinkBehaviour.KillOnDestroy);
        t.SetId(ResolveId(id, target));
        return t;
    }

    /// <summary>
    /// 颜色切换（迅速）：立刻换色，像“啪”地换了一张皮肤。
    /// </summary>
    public void ColorInstant(Graphic graphic, Color to)
    {
        if (graphic == null) return;
        graphic.color = to;
    }

    /// <summary>
    /// 颜色切换（渐变）：从当前颜色慢慢染到目标颜色，像“颜料渗进去”。
    /// </summary>
    public Tween ColorFade(Graphic graphic, Color to, float duration = 0.12f, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (graphic == null) return null;
        if (ResolveKill(killTargetTweens)) graphic.DOKill();

        Tween t = graphic.DOColor(to, duration).SetEase(YusEase.FastToSlow);
        t.SetUpdate(ResolveUnscaledForUI(unscaledTime));
        t.SetLink(graphic.gameObject, LinkBehaviour.KillOnDestroy);
        t.SetId(ResolveId(id, graphic));
        return t;
    }

    public void ColorInstant(SpriteRenderer renderer, Color to)
    {
        if (renderer == null) return;
        renderer.color = to;
    }

    public Tween ColorFade(SpriteRenderer renderer, Color to, float duration = 0.12f, bool? unscaledTime = null, bool? killTargetTweens = null, object id = null)
    {
        if (renderer == null) return null;
        if (ResolveKill(killTargetTweens)) renderer.DOKill();

        Tween t = renderer.DOColor(to, duration).SetEase(YusEase.FastToSlow);
        t.SetUpdate(unscaledTime ?? false);
        t.SetLink(renderer.gameObject, LinkBehaviour.KillOnDestroy);
        t.SetId(ResolveId(id, renderer));
        return t;
    }

    /// <summary>
    /// Renderer 换色（迅速）：默认改 instance material（renderer.material），避免一不小心把全体都染了。
    /// </summary>
    public void ColorInstant(Renderer renderer, Color to, bool useSharedMaterial = false)
    {
        if (renderer == null) return;
        Material m = useSharedMaterial ? renderer.sharedMaterial : renderer.material;
        if (m == null) return;
        if (m.HasProperty("_Color")) m.color = to;
    }

    /// <summary>
    /// Renderer 换色（渐变）：适合受击闪白、状态变色、环境色过渡。
    /// </summary>
    public Tween ColorFade(Renderer renderer, Color to, float duration = 0.12f, bool? unscaledTime = null, bool? killTargetTweens = null, bool useSharedMaterial = false, object id = null)
    {
        if (renderer == null) return null;
        if (ResolveKill(killTargetTweens)) renderer.DOKill();

        Material m = useSharedMaterial ? renderer.sharedMaterial : renderer.material;
        if (m == null) return null;
        if (!m.HasProperty("_Color")) return null;

        Tween t = m.DOColor(to, duration).SetEase(YusEase.FastToSlow);
        t.SetUpdate(unscaledTime ?? false);
        t.SetLink(renderer.gameObject, LinkBehaviour.KillOnDestroy);
        t.SetId(ResolveId(id, renderer));
        return t;
    }
}

#endif
