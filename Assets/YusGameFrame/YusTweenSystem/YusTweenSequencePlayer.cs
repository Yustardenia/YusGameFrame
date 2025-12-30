#if YUS_DOTWEEN
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A lightweight, Inspector-first tween sequencer for gameplay/UI.
/// - Designers can "compose" multiple tracks (move/scale/rotate/color/fade) in the Inspector.
/// - Uses YusTween to standardize common defaults (unscaled time, link-on-destroy, id).
/// </summary>
[DisallowMultipleComponent]
public class YusTweenSequencePlayer : MonoBehaviour
{
    public enum PlayTrigger
    {
        Manual = 0,
        OnEnable = 1,
        Start = 2,
    }

    public enum TrackType
    {
        LocalMove = 0,
        AnchoredPosition = 1,
        LocalRotate = 2,
        LocalScale = 3,
        CanvasGroupAlpha = 4,
        GraphicColor = 5,
        SpriteRendererColor = 6,
        RendererColor = 7,
    }

    [Serializable]
    public class Track
    {
        public bool enabled = true;
        public TrackType type = TrackType.LocalScale;

        [Min(0f)] public float delay = 0f;
        [Min(0.001f)] public float duration = 0.2f;
        public Ease ease = YusEase.CleanStop;

        public int loops = 0;
        public LoopType loopType = LoopType.Restart;

        public bool setFrom = false;
        public bool relative = false;

        public Vector3 fromVector3 = Vector3.zero;
        public Vector3 toVector3 = Vector3.one;

        public Vector2 fromVector2 = Vector2.zero;
        public Vector2 toVector2 = Vector2.zero;

        [Range(0f, 1f)] public float fromAlpha = 0f;
        [Range(0f, 1f)] public float toAlpha = 1f;

        public Color fromColor = Color.white;
        public Color toColor = Color.white;

        public Transform transformTarget;
        public RectTransform rectTransformTarget;
        public CanvasGroup canvasGroupTarget;
        public Graphic graphicTarget;
        public SpriteRenderer spriteRendererTarget;
        public Renderer rendererTarget;
        public bool rendererUseSharedMaterial = false;

        public bool killTargetTweens = false;
    }

    [SerializeField] private PlayTrigger playTrigger = PlayTrigger.Manual;
    [SerializeField] private bool stopOnDisable = true;

    [SerializeField] private bool unscaledTime = true;
    [SerializeField] private LinkBehaviour linkBehaviour = LinkBehaviour.KillOnDestroy;
    [SerializeField] private bool autoKill = true;
    [SerializeField] private bool killPreviousOnPlay = true;

    [SerializeField] private List<Track> tracks = new List<Track>();

    private Sequence _sequence;

    public IReadOnlyList<Track> Tracks => tracks;

    private void OnEnable()
    {
        if (playTrigger == PlayTrigger.OnEnable) PlayForward();
    }

    private void Start()
    {
        if (playTrigger == PlayTrigger.Start) PlayForward();
    }

    private void OnDisable()
    {
        if (stopOnDisable) Stop();
    }

    public void PlayForward()
    {
        BuildSequence(restart: true);
        _sequence?.PlayForward();
    }

    public void PlayBackwardFromEnd()
    {
        BuildSequence(restart: false);
        if (_sequence == null) return;

        _sequence.Goto(_sequence.Duration(includeLoops: false), andPlay: false);
        _sequence.PlayBackwards();
    }

    public void Stop(bool complete = false)
    {
        if (_sequence == null) return;
        _sequence.Kill(complete);
        _sequence = null;
    }

    public void Rewind()
    {
        if (_sequence == null) return;
        _sequence.Rewind();
    }

    [ContextMenu("YusTween/Play Forward")]
    private void ContextPlayForward() => PlayForward();

    [ContextMenu("YusTween/Play Backward From End")]
    private void ContextPlayBackward() => PlayBackwardFromEnd();

    [ContextMenu("YusTween/Stop")]
    private void ContextStop() => Stop();

    [ContextMenu("YusTween/Auto Bind Targets")]
    public void AutoBindTargets()
    {
        foreach (var track in tracks)
        {
            if (track == null) continue;

            if (track.transformTarget == null) track.transformTarget = transform;
            if (track.rectTransformTarget == null) track.rectTransformTarget = GetComponent<RectTransform>();
            if (track.canvasGroupTarget == null) track.canvasGroupTarget = GetComponent<CanvasGroup>();
            if (track.graphicTarget == null) track.graphicTarget = GetComponent<Graphic>();
            if (track.spriteRendererTarget == null) track.spriteRendererTarget = GetComponent<SpriteRenderer>();
            if (track.rendererTarget == null) track.rendererTarget = GetComponent<Renderer>();
        }
    }

    private void BuildSequence(bool restart)
    {
        if (!restart && _sequence != null && _sequence.active) return;

        if (killPreviousOnPlay) YusTween.KillId(this);
        Stop();

        Sequence seq = YusTween.NewSequence(unscaledTime: unscaledTime, id: this, linkGameObject: gameObject, linkBehaviour: linkBehaviour);
        seq.SetAutoKill(autoKill);

        for (int i = 0; i < tracks.Count; i++)
        {
            Track track = tracks[i];
            if (track == null || !track.enabled) continue;

            Tween tween = CreateTrackTween(track);
            if (tween == null) continue;

            seq.Insert(Mathf.Max(0f, track.delay), tween);
        }

        _sequence = seq;
    }

    private Tween CreateTrackTween(Track track)
    {
        Tween tween;

        switch (track.type)
        {
            case TrackType.LocalMove:
            {
                Transform t = track.transformTarget;
                if (t == null) return null;
                if (track.killTargetTweens) t.DOKill();
                if (track.setFrom) t.localPosition = track.fromVector3;
                tween = t.DOLocalMove(track.toVector3, Mathf.Max(0.001f, track.duration));
                break;
            }
            case TrackType.AnchoredPosition:
            {
                RectTransform rt = track.rectTransformTarget;
                if (rt == null) return null;
                if (track.killTargetTweens) rt.DOKill();
                if (track.setFrom) rt.anchoredPosition = track.fromVector2;
                tween = rt.DOAnchorPos(track.toVector2, Mathf.Max(0.001f, track.duration));
                break;
            }
            case TrackType.LocalRotate:
            {
                Transform t = track.transformTarget;
                if (t == null) return null;
                if (track.killTargetTweens) t.DOKill();
                if (track.setFrom) t.localEulerAngles = track.fromVector3;
                tween = t.DOLocalRotate(track.toVector3, Mathf.Max(0.001f, track.duration), RotateMode.Fast);
                break;
            }
            case TrackType.LocalScale:
            {
                Transform t = track.transformTarget;
                if (t == null) return null;
                if (track.killTargetTweens) t.DOKill();
                if (track.setFrom) t.localScale = track.fromVector3;
                tween = t.DOScale(track.toVector3, Mathf.Max(0.001f, track.duration));
                break;
            }
            case TrackType.CanvasGroupAlpha:
            {
                CanvasGroup cg = track.canvasGroupTarget;
                if (cg == null) return null;
                if (track.killTargetTweens) cg.DOKill();
                if (track.setFrom) cg.alpha = track.fromAlpha;
                tween = cg.DOFade(track.toAlpha, Mathf.Max(0.001f, track.duration));
                break;
            }
            case TrackType.GraphicColor:
            {
                Graphic g = track.graphicTarget;
                if (g == null) return null;
                if (track.killTargetTweens) g.DOKill();
                if (track.setFrom) g.color = track.fromColor;
                tween = g.DOColor(track.toColor, Mathf.Max(0.001f, track.duration));
                break;
            }
            case TrackType.SpriteRendererColor:
            {
                SpriteRenderer sr = track.spriteRendererTarget;
                if (sr == null) return null;
                if (track.killTargetTweens) sr.DOKill();
                if (track.setFrom) sr.color = track.fromColor;
                tween = sr.DOColor(track.toColor, Mathf.Max(0.001f, track.duration));
                break;
            }
            case TrackType.RendererColor:
            {
                Renderer r = track.rendererTarget;
                if (r == null) return null;
                if (track.killTargetTweens) r.DOKill();

                Material m = track.rendererUseSharedMaterial ? r.sharedMaterial : r.material;
                if (m == null || !m.HasProperty("_Color")) return null;

                if (track.setFrom) m.color = track.fromColor;
                tween = m.DOColor(track.toColor, Mathf.Max(0.001f, track.duration));
                break;
            }
            default:
                return null;
        }

        if (track.relative) tween.SetRelative();
        tween.SetEase(track.ease);

        if (track.loops != 0)
        {
            tween.SetLoops(track.loops, track.loopType);
        }

        return tween;
    }
}
#endif
