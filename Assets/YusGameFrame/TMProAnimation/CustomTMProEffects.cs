using System;
using System.Collections.Generic;
using Fungus;
using Fungus.TMProLinkAnimEffects;
using TMPro;
using UnityEngine;

namespace YusGameFrame
{
    /// <summary>
    /// 自定义 TextMeshPro link 动画效果注册类（基于 Fungus 的 TMProLinkAnimator）
    /// 用法：在 TMP 文本中写 <link="id">内容</link>
    /// </summary>
    public class CustomTMProEffects
    {
        private static bool _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterCustomEffects_BeforeSceneLoad()
        {
            RegisterCustomEffects();
        }

        // Fallback for setups where BeforeSceneLoad doesn't run as expected (e.g. certain play mode options).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterCustomEffects_AfterSceneLoad()
        {
            RegisterCustomEffects();
        }

        public static void RegisterCustomEffects()
        {
            if (_registered)
                return;

            // 1) heartbeat：心跳缩放（按词）
            TMProLinkAnimLookup.AddHelper("heartbeat", new PulseEffect
            {
                mode = TMPLinkAnimatorMode.PerWord,
                speed = 8f,
                scale = new Vector3(0.15f, 0.15f, 0),
                hueScale = 0,
                saturationScale = 0,
                valueScale = 0
            });

            // 2) spin：旋转（按字）
            TMProLinkAnimLookup.AddHelper("spin", new PivotEffect
            {
                mode = TMPLinkAnimatorMode.PerCharacter,
                speed = 2f,
                degScale = 20f
            });

            // 3) rain：下落（按字）
            TMProLinkAnimLookup.AddHelper("rain", new AscendEffect
            {
                mode = TMPLinkAnimatorMode.PerCharacter,
                totalStep = -0.5f
            });

            // 4) glitch：故障（按字）
            TMProLinkAnimLookup.AddHelper("glitch", new GlitchEffect
            {
                mode = TMPLinkAnimatorMode.PerCharacter,
                intensity = 2.0f,
                speed = 15f
            });

            // 5) type：打字机（按字逐个出现）
	            TMProLinkAnimLookup.AddHelper("type", new TypewriterEffect
	            {
	                mode = TMPLinkAnimatorMode.PerCharacter,
	                // Default is intentionally slow enough to be visible for short strings.
	                charactersPerSecond = 8f,
	                startDelay = 0f,
	                fadeInDuration = 0.05f,
	                useUnscaledTime = true
	            });

	            // 6) garble：动态乱码（按字；始终显示乱码，不显示原文字形）
	            TMProLinkAnimLookup.AddHelper("garble", new GarbleEffect
	            {
	                mode = TMPLinkAnimatorMode.PerCharacter,
	                speed = 25f,
	                useUnscaledTime = true
	            });

	            _registered = true;
	        }

        public class GlitchEffect : BaseEffect
        {
            public float intensity = 1f;
            public float speed = 10f;

            public override Matrix4x4 TransFunc(int index)
            {
                float timeStep = Mathf.Floor(Time.time * speed);

                float rX = (Mathf.Sin(timeStep + index * 13.0f) > 0.5f ? 1 : -1) * Mathf.Sin(timeStep * 1.1f) * intensity * 0.05f;
                float rY = (Mathf.Cos(timeStep * 0.7f + index * 17.0f) > 0.5f ? 1 : -1) * Mathf.Cos(timeStep * 1.3f) * intensity * 0.05f;

                float scaleDistort = (Mathf.Sin(timeStep * 0.3f + index) > 0.95f) ? 1.5f : 1.0f;
                return Matrix4x4.TRS(new Vector3(rX, rY, 0), Quaternion.identity, new Vector3(scaleDistort, 1, 1));
            }

            public override Color32 ColorFunc(int index, Color32 col)
            {
                float timeStep = Mathf.Floor(Time.time * speed);
                if (Mathf.Sin(timeStep * 0.8f + index * 2) > 0.9f)
                {
                    return new Color32(255, 0, 0, 255);
                }
                return col;
            }
        }

	        public class TypewriterEffect : BaseEffect
	        {
            [Tooltip("打字速度：每秒显示多少个字符")]
            public float charactersPerSecond = 30f;

            [Tooltip("开始前延迟（秒）")]
            public float startDelay = 0f;

            [Tooltip("每个字符的淡入时长（0 则直接显示）")]
            public float fadeInDuration = 0f;

            [Tooltip("是否使用 unscaledTime（不受 Time.timeScale 影响）")]
            public bool useUnscaledTime = true;

            private struct LinkKey : IEquatable<LinkKey>
            {
                public int contextId;
                public int start;
                public int length;

                public LinkKey(int contextId, int start, int length)
                {
                    this.contextId = contextId;
                    this.start = start;
                    this.length = length;
                }

                public bool Equals(LinkKey other) => contextId == other.contextId && start == other.start && length == other.length;
                public override bool Equals(object obj) => obj is LinkKey other && Equals(other);
                public override int GetHashCode()
                {
                    unchecked
                    {
                        int hash = contextId;
                        hash = (hash * 397) ^ start;
                        hash = (hash * 397) ^ length;
                        return hash;
                    }
                }
            }

            private struct TypewriterSettings
            {
                public float charactersPerSecond;
                public float startDelay;
                public float fadeInDuration;
            }

            private readonly Dictionary<LinkKey, float> _startTimeByLink = new Dictionary<LinkKey, float>();
            private readonly Dictionary<LinkKey, float> _lastSeenTimeByLink = new Dictionary<LinkKey, float>();
            private readonly Dictionary<LinkKey, TypewriterSettings> _settingsByLink = new Dictionary<LinkKey, TypewriterSettings>();
            private readonly Dictionary<int, string> _lastTextByContext = new Dictionary<int, string>();

            private float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

            private bool TryGetSettings(out TypewriterSettings settings)
            {
                int ctxId = CurrentContext != null ? CurrentContext.GetInstanceID() : 0;
                var key = new LinkKey(ctxId, CurrentStart, CurrentLength);

                if (_settingsByLink.TryGetValue(key, out settings))
                    return true;

                settings = new TypewriterSettings
                {
                    charactersPerSecond = charactersPerSecond,
                    startDelay = startDelay,
                    fadeInDuration = fadeInDuration
                };
                return false;
            }

            private int GetRevealCount()
            {
                int ctxId = CurrentContext != null ? CurrentContext.GetInstanceID() : 0;
                var key = new LinkKey(ctxId, CurrentStart, CurrentLength);

                if (!_startTimeByLink.TryGetValue(key, out var startTime))
                    return 0;

                TryGetSettings(out var settings);

                float cps = Mathf.Max(0.01f, settings.charactersPerSecond);
                float elapsed = (Now - startTime) - Mathf.Max(0f, settings.startDelay);
                if (elapsed < 0f)
                    return 0;

                return Mathf.FloorToInt(elapsed * cps);
            }

            private void UpdateSettingsFromLinkId(TMProLinkAnimator context, LinkKey key)
            {
                if (context == null || context.TMProComponent == null)
                    return;

                var textInfo = context.TMProComponent.textInfo;
                if (textInfo == null)
                    return;

                string linkId = null;
                for (int i = 0; i < textInfo.linkCount; i++)
                {
                    var li = textInfo.linkInfo[i];
                    if (li.linkTextfirstCharacterIndex == key.start && li.linkTextLength == key.length)
                    {
                        linkId = li.GetLinkID();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(linkId))
                    return;

                if (!TryParseTypewriterParams(linkId, out var parsed))
                    return;

                _settingsByLink[key] = parsed;
            }

            // Supported:
            // - "type:12" -> cps
            // - "type:12,0.2" -> cps, startDelay
            // - "type:12,0.2,0.05" -> cps, startDelay, fadeInDuration
            // Also accepts "type=..." and "type(...)".
            private bool TryParseTypewriterParams(string linkIdRaw, out TypewriterSettings settings)
            {
                settings = new TypewriterSettings
                {
                    charactersPerSecond = charactersPerSecond,
                    startDelay = startDelay,
                    fadeInDuration = fadeInDuration
                };

                if (string.IsNullOrEmpty(linkIdRaw))
                    return false;

                string linkId = linkIdRaw.Trim();
                if (linkId.Length >= 2)
                {
                    char first = linkId[0];
                    char last = linkId[linkId.Length - 1];
                    if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                        linkId = linkId.Substring(1, linkId.Length - 2);
                }

                int sep = linkId.IndexOfAny(new[] { ':', '=', '(' });
                if (sep < 0)
                    return false;

                string prefix = linkId.Substring(0, sep).Trim();
                if (!string.Equals(prefix, "type", StringComparison.OrdinalIgnoreCase))
                    return false;

                string paramPart = linkId.Substring(sep + 1).Trim();
                if (paramPart.EndsWith(")"))
                    paramPart = paramPart.Substring(0, paramPart.Length - 1).Trim();

                if (string.IsNullOrEmpty(paramPart))
                    return false;

                string[] parts = paramPart.Split(',');
                if (parts.Length >= 1 && float.TryParse(parts[0].Trim(), out var cps))
                    settings.charactersPerSecond = cps;
                if (parts.Length >= 2 && float.TryParse(parts[1].Trim(), out var delay))
                    settings.startDelay = delay;
                if (parts.Length >= 3 && float.TryParse(parts[2].Trim(), out var fade))
                    settings.fadeInDuration = fade;

                return true;
            }

            public override void DoEffect(TMProLinkAnimator context, int start, int length)
            {
                CurrentContext = context;
                CurrentStart = start;
                CurrentLength = length;

                int ctxId = context != null ? context.GetInstanceID() : 0;
                string curText = context != null && context.TMProComponent != null ? context.TMProComponent.text : string.Empty;

                if (_lastTextByContext.TryGetValue(ctxId, out var lastText))
                {
                    if (!string.Equals(lastText, curText, StringComparison.Ordinal))
                    {
                        ResetContext(ctxId);
                        _lastTextByContext[ctxId] = curText;
                    }
                }
                else
                {
                    _lastTextByContext[ctxId] = curText;
                }

                var key = new LinkKey(ctxId, start, length);
                float now = Now;

                if (!_startTimeByLink.TryGetValue(key, out var startTime))
                {
                    startTime = now;
                }
                else
                {
                    // Domain reload disabled can keep old start times; reset if time moved backwards.
                    if (now + 0.01f < startTime)
                        startTime = now;

                    // If the link wasn't processed for a while (object disabled, scene change, etc.), restart.
                    if (_lastSeenTimeByLink.TryGetValue(key, out var lastSeen) && now - lastSeen > 0.5f)
                        startTime = now;
                }

                _startTimeByLink[key] = startTime;
                _lastSeenTimeByLink[key] = now;
                UpdateSettingsFromLinkId(context, key);

                MeshVertUpdateLoop(context, start, length, TransFunc, ColorFunc, mode);
            }

            private void ResetContext(int ctxId)
            {
                if (_startTimeByLink.Count == 0) return;
                var toRemove = new List<LinkKey>();
                foreach (var kv in _startTimeByLink)
                {
                    if (kv.Key.contextId == ctxId)
                        toRemove.Add(kv.Key);
                }
                for (int i = 0; i < toRemove.Count; i++)
                    _startTimeByLink.Remove(toRemove[i]);
                for (int i = 0; i < toRemove.Count; i++)
                    _lastSeenTimeByLink.Remove(toRemove[i]);
                for (int i = 0; i < toRemove.Count; i++)
                    _settingsByLink.Remove(toRemove[i]);
            }

            public override Color32 ColorFunc(int index, Color32 col)
            {
                int revealCount = GetRevealCount();
                int relIndex = index - CurrentStart;

                if (relIndex < 0)
                    return col;
                if (relIndex >= revealCount)
                    return new Color32(col.r, col.g, col.b, 0);

                if (fadeInDuration <= 0f)
                    return col;

                TryGetSettings(out var settings);

                float cps = Mathf.Max(0.01f, settings.charactersPerSecond);
                int ctxId = CurrentContext != null ? CurrentContext.GetInstanceID() : 0;
                var key = new LinkKey(ctxId, CurrentStart, CurrentLength);
                _startTimeByLink.TryGetValue(key, out var startTime);
                float elapsed = (Now - startTime) - Mathf.Max(0f, settings.startDelay);
                float charElapsed = elapsed - (relIndex / cps);
                float a = Mathf.Clamp01(charElapsed / Mathf.Max(0.0001f, settings.fadeInDuration));
                byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(col.a * a), 0, 255);
                return new Color32(col.r, col.g, col.b, alpha);
            }

	            public override Matrix4x4 TransFunc(int index)
	            {
	                // Some TMP shaders / settings can make alpha-only fades hard to notice.
	                // Also hide unrevealed characters by scaling them down.
	                int revealCount = GetRevealCount();
	                int relIndex = index - CurrentStart;
	                if (relIndex < 0)
	                    return Matrix4x4.identity;
	                if (relIndex >= revealCount)
	                    return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.zero);
	                return Matrix4x4.identity;
	            }
	        }

	        public class GarbleEffect : BaseEffect
	        {
	            [Tooltip("每秒变化次数（越大越抖）")]
	            public float speed = 20f;

	            [Tooltip("是否使用 unscaledTime（不受 Time.timeScale 影响）")]
	            public bool useUnscaledTime = true;

	            [Tooltip("乱码字符集合（会根据字体实际包含的字符过滤）")]
	            public string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+[]{};:,.?/\\\\|~";

	            private readonly Dictionary<int, string> _baseTextByContext = new Dictionary<int, string>();
	            private readonly Dictionary<int, string> _lastAppliedByContext = new Dictionary<int, string>();
	            private readonly Dictionary<int, int> _lastStepByContext = new Dictionary<int, int>();

	            private float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

	            public override void DoEffect(TMProLinkAnimator context, int start, int length)
	            {
	                CurrentContext = context;
	                CurrentStart = start;
	                CurrentLength = length;

	                if (context == null || context.TMProComponent == null)
	                    return;

	                if (string.IsNullOrEmpty(charset))
	                    return;

	                var tmproComponent = context.TMProComponent;
	                int ctxId = tmproComponent.GetInstanceID();

	                // Detect external text changes (anything not set by us) and refresh base text.
	                string currentText = tmproComponent.text ?? string.Empty;
	                if (!_lastAppliedByContext.TryGetValue(ctxId, out var lastApplied) ||
	                    !string.Equals(lastApplied, currentText, StringComparison.Ordinal))
	                {
	                    _baseTextByContext[ctxId] = currentText;
	                    _lastAppliedByContext[ctxId] = currentText;
	                    _lastStepByContext.Remove(ctxId);
	                }

	                int step = Mathf.FloorToInt(Now * Mathf.Max(0.01f, speed));
	                if (_lastStepByContext.TryGetValue(ctxId, out var lastStep) && lastStep == step)
	                    return;

	                if (!_baseTextByContext.TryGetValue(ctxId, out var baseText))
	                    baseText = currentText;

	                var textInfo = tmproComponent.textInfo;
	                if (textInfo == null || textInfo.characterInfo == null || textInfo.characterCount == 0)
	                    return;

	                var chars = baseText.ToCharArray();
	                int end = start + length;

	                int safeStart = Mathf.Clamp(start, 0, textInfo.characterCount);
	                int safeEnd = Mathf.Clamp(end, 0, textInfo.characterCount);

	                for (int i = safeStart; i < safeEnd; i++)
	                {
	                    var ci = textInfo.characterInfo[i];
	                    if (!ci.isVisible) continue;

	                    int pos = ci.index;
	                    int len = Mathf.Max(1, ci.stringLength);
	                    for (int j = 0; j < len; j++)
	                    {
	                        int p = pos + j;
	                        if ((uint)p >= (uint)chars.Length) continue;
	                        chars[p] = charset[Hash(step, i, j) % charset.Length];
	                    }
	                }

	                string garbled = new string(chars);
	                try
	                {
	                    // Prevent recursion via TMPro_EventManager.TEXT_CHANGED_EVENT.
	                    Fungus.TMProLinkAnimator.BeginSuppressTextChangedEvent();
	                    tmproComponent.text = garbled;
	                }
	                finally
	                {
	                    Fungus.TMProLinkAnimator.EndSuppressTextChangedEvent();
	                }

	                _lastAppliedByContext[ctxId] = garbled;
	                _lastStepByContext[ctxId] = step;

	                // Ensure the animator refreshes its cached mesh next frame.
	                context.SetDirty();
	            }

	            private static int Hash(int a, int b, int c)
	            {
	                unchecked
	                {
	                    int h = 17;
	                    h = (h * 31) ^ a;
	                    h = (h * 31) ^ b;
	                    h = (h * 31) ^ c;
	                    h ^= (h << 13);
	                    h ^= (h >> 17);
	                    h ^= (h << 5);
	                    return (h & 0x7fffffff);
	                }
	            }
	        }
	    }
	}
