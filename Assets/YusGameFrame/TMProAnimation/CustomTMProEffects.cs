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
        private static bool _didLogRegisterState;
        private static int _runtimeInitSessionId;
        private static int _lastRegisterSessionId = int.MinValue;

        // Runs when entering Play Mode even with "Disable Domain Reload" (Unity uses this hook to let you reset statics).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            unchecked { _runtimeInitSessionId++; }
            _lastRegisterSessionId = int.MinValue;
            _didLogRegisterState = false;
            _registered = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterCustomEffects_BeforeSceneLoad()
        {
            RegisterCustomEffects(_runtimeInitSessionId);
        }

        // Fallback for setups where BeforeSceneLoad doesn't run as expected (e.g. certain play mode options).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterCustomEffects_AfterSceneLoad()
        {
            RegisterCustomEffects(_runtimeInitSessionId);
        }

        public static void RegisterCustomEffects()
        {
            RegisterCustomEffects(_runtimeInitSessionId);
        }

        private static void RegisterCustomEffects(int sessionId)
        {
            // Prevent double registration within the same play-mode start (we have both BeforeSceneLoad and AfterSceneLoad hooks).
            if (_lastRegisterSessionId == sessionId && AreAllCustomEffectsRegistered())
                return;
            _lastRegisterSessionId = sessionId;

            // Domain Reload disabled can keep our static flag but not necessarily keep the lookup dictionary
            // in a consistent state across script recompiles / play mode transitions.
            // Since AddHelper is idempotent (ignores duplicates), we can safely re-register when needed.
            RemoveCustomEffectsFromLookup();
            if (_registered && AreAllCustomEffectsRegistered())
            {
                DebugLogRegisterStateOnce("re-register (override)");
            }

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

	            // 7) shine：高光扫过（按字）
	            TMProLinkAnimLookup.AddHelper("shine", new ShineEffect
	            {
	                mode = TMPLinkAnimatorMode.PerCharacter,
	                duration = 0.9f,
	                intensity = 1.0f,
	                width01 = 0.18f,
	                // White-on-white is hard to see; default to a warm highlight tint.
	                shineColor = new Color(1f, 0.92f, 0.55f, 1f),
	                useUnscaledTime = true
	            });

	            // 8) flicker：霓虹闪烁（按字）
	            TMProLinkAnimLookup.AddHelper("flicker", new FlickerEffect
	            {
	                mode = TMPLinkAnimatorMode.PerCharacter,
	                speed = 18f,
	                onProbability = 0.4f,
	                brightness = 2.2f,
	                hueJitter = 0.06f,
	                saturationBoost = 0.28f,
	                valueBoost = 0.55f,
	                minAlpha = 0.12f,
	                minScale = 0.85f,
	                yOffset = 7.0f,
	                useUnscaledTime = true
	            });

	            // 9) float：轻微漂浮/呼吸（按字）
	            TMProLinkAnimLookup.AddHelper("float", new FloatEffect
	            {
	                mode = TMPLinkAnimatorMode.PerCharacter,
	                speed = 2.0f,
	                indexStep = 0.35f,
	                // UGUI TMP coordinates are typically in font-size space; use a visible default offset.
	                amplitude = 2.5f,
	                scale = new Vector3(0.06f, 0.06f, 0f),
	                useUnscaledTime = true
	            });

	            // 10) impact：入场“砰”一下（按段）
	            TMProLinkAnimLookup.AddHelper("impact", new ImpactEffect
	            {
	                mode = TMPLinkAnimatorMode.PerSection,
	                duration = 0.55f,
	                frequency = 9f,
	                amplitude = 0.35f,
	                yAmplitude = 3.0f,
	                damping = 9f,
	                useUnscaledTime = true
	            });

	            // 11) reveal：擦除显现（按字/按位置）
	            TMProLinkAnimLookup.AddHelper("reveal", new RevealEffect
	            {
	                mode = TMPLinkAnimatorMode.PerCharacter,
	                duration = 0.7f,
	                feather01 = 0.06f,
	                useUnscaledTime = true
	            });

	            _registered = true;
	            DebugLogRegisterStateOnce("registered");
	        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void DebugLogRegisterStateOnce(string reason)
        {
            if (_didLogRegisterState)
                return;

            _didLogRegisterState = true;

            bool ok = AreAllCustomEffectsRegistered();
            int count = TMProLinkAnimLookup.LinkHashToEffect != null ? TMProLinkAnimLookup.LinkHashToEffect.Count : -1;

            Debug.Log(
                $"[TMProEffects] RegisterCustomEffects: {reason} | _registered={_registered} | allCustomRegistered={ok} | lookupCount={count}\n" +
                $"[TMProEffects] keys: heartbeat={HasHelper("heartbeat")}, spin={HasHelper("spin")}, rain={HasHelper("rain")}, glitch={HasHelper("glitch")}, type={HasHelper("type")}, garble={HasHelper("garble")}, " +
                $"shine={HasHelper("shine")}, flicker={HasHelper("flicker")}, float={HasHelper("float")}, impact={HasHelper("impact")}, reveal={HasHelper("reveal")}"
            );
        }

        private static bool AreAllCustomEffectsRegistered()
        {
            if (TMProLinkAnimLookup.LinkHashToEffect == null)
                return false;

            return HasHelper("heartbeat")
                   && HasHelper("spin")
                   && HasHelper("rain")
                   && HasHelper("glitch")
                   && HasHelper("type")
                   && HasHelper("garble")
                   && HasHelper("shine")
                   && HasHelper("flicker")
                   && HasHelper("float")
                   && HasHelper("impact")
                   && HasHelper("reveal");
        }

        private static bool HasHelper(string id)
        {
            int hash = TMP_TextUtilities.GetSimpleHashCode(id);
            return TMProLinkAnimLookup.LinkHashToEffect.ContainsKey(hash);
        }

        private static void RemoveCustomEffectsFromLookup()
        {
            // Only remove the helpers we own; keep Fungus defaults intact.
            TMProLinkAnimLookup.Remove("heartbeat");
            TMProLinkAnimLookup.Remove("spin");
            TMProLinkAnimLookup.Remove("rain");
            TMProLinkAnimLookup.Remove("glitch");
            TMProLinkAnimLookup.Remove("type");
            TMProLinkAnimLookup.Remove("garble");
            TMProLinkAnimLookup.Remove("shine");
            TMProLinkAnimLookup.Remove("flicker");
            TMProLinkAnimLookup.Remove("float");
            TMProLinkAnimLookup.Remove("impact");
            TMProLinkAnimLookup.Remove("reveal");
            // "decode" was removed; ensure any previous registration is cleared (important when Domain Reload is disabled).
            TMProLinkAnimLookup.Remove("decode");
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

	        public class ShineEffect : BaseEffect
	        {
	            [Tooltip("一次扫光时长（秒）。超过该时间后不再重复播放。")]
	            public float duration = 0.9f;

	            [Tooltip("扫光强度（0-1）")]
	            public float intensity = 0.85f;

	            [Tooltip("扫光宽度（0-1，按本段宽度归一化）")]
	            public float width01 = 0.12f;

	            public Color shineColor = Color.white;
	            public bool useUnscaledTime = true;

	            private float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

	            private float _minX;
	            private float _maxX;

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

	            private readonly Dictionary<LinkKey, float> _startTimeByLink = new Dictionary<LinkKey, float>();
	            private readonly Dictionary<LinkKey, float> _lastSeenTimeByLink = new Dictionary<LinkKey, float>();
	            private readonly Dictionary<int, string> _lastTextByContext = new Dictionary<int, string>();

	            public override void DoEffect(TMProLinkAnimator context, int start, int length)
	            {
	                CurrentContext = context;
	                CurrentStart = start;
	                CurrentLength = length;

	                if (context == null || context.TMProComponent == null)
	                    return;

	                int ctxId = context.GetInstanceID();
	                string curText = context.TMProComponent.text ?? string.Empty;
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
	                    if (now + 0.01f < startTime)
	                        startTime = now;
	                    if (_lastSeenTimeByLink.TryGetValue(key, out var lastSeen) && now - lastSeen > 0.5f)
	                        startTime = now;
	                }
	                _startTimeByLink[key] = startTime;
	                _lastSeenTimeByLink[key] = now;

	                ComputeMinMaxX(context.TMProComponent.textInfo, start, length, out _minX, out _maxX);
	                MeshVertUpdateLoop(context, start, length, TransFunc, ColorFunc, mode);
	            }

	            public override Color32 ColorFunc(int index, Color32 col)
	            {
	                float elapsed = GetElapsed();
	                float dur = Mathf.Max(0.01f, duration);
	                if (elapsed < 0f || elapsed >= dur)
	                    return col;

	                if (CurrentContext == null || CurrentContext.TMProComponent == null)
	                    return col;

	                var textInfo = CurrentContext.TMProComponent.textInfo;
	                if (textInfo == null || textInfo.characterInfo == null || (uint)index >= (uint)textInfo.characterCount)
	                    return col;

	                float range = Mathf.Max(0.0001f, _maxX - _minX);
	                float x = (textInfo.characterInfo[index].bottomLeft.x + textInfo.characterInfo[index].topRight.x) * 0.5f;
	                float pos01 = Mathf.Clamp01((x - _minX) / range);

	                float phase = Mathf.Clamp01(elapsed / dur);
	                float d = Mathf.Abs(pos01 - phase);

	                float w = Mathf.Max(0.005f, width01);
	                float k = Mathf.Exp(-(d * d) / (2f * w * w)) * Mathf.Clamp01(intensity);

	                Color baseCol = col;
	                Color target = shineColor;
	                Color result = Color.Lerp(baseCol, target, k);
	                result.a = baseCol.a;
	                return (Color32)result;
	            }

	            private static void ComputeMinMaxX(TMP_TextInfo textInfo, int start, int length, out float minX, out float maxX)
	            {
	                minX = 0f;
	                maxX = 0f;
	                if (textInfo == null || textInfo.characterInfo == null || textInfo.characterCount == 0)
	                    return;

	                int end = start + length;
	                int safeStart = Mathf.Clamp(start, 0, textInfo.characterCount);
	                int safeEnd = Mathf.Clamp(end, 0, textInfo.characterCount);

	                bool hasAny = false;
	                for (int i = safeStart; i < safeEnd; i++)
	                {
	                    var ci = textInfo.characterInfo[i];
	                    if (!ci.isVisible) continue;

	                    if (!hasAny)
	                    {
	                        minX = ci.bottomLeft.x;
	                        maxX = ci.topRight.x;
	                        hasAny = true;
	                    }
	                    else
	                    {
	                        minX = Mathf.Min(minX, ci.bottomLeft.x);
	                        maxX = Mathf.Max(maxX, ci.topRight.x);
	                    }
	                }
	            }

	            private float GetElapsed()
	            {
	                int ctxId = CurrentContext != null ? CurrentContext.GetInstanceID() : 0;
	                var key = new LinkKey(ctxId, CurrentStart, CurrentLength);
	                if (!_startTimeByLink.TryGetValue(key, out var startTime))
	                    return -1f;
	                return Now - startTime;
	            }

	            private void ResetContext(int ctxId)
	            {
	                if (_startTimeByLink.Count == 0)
	                    return;

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
	            }
	        }

	        public class FlickerEffect : BaseEffect
	        {
	            [Tooltip("闪烁速度（每秒切换次数）")]
	            public float speed = 18f;

	            [Tooltip("亮起概率（0-1，越大越容易亮）")]
	            public float onProbability = 0.55f;

	            public float brightness = 1.6f;
	            public float hueJitter = 0.03f;
	            public float saturationBoost = 0.15f;
	            public float valueBoost = 0.35f;
	            public float minAlpha = 0.35f;
	            public float minScale = 0.92f;
	            public float yOffset = 3.0f;
	            public bool useUnscaledTime = true;

	            private float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

	            public override Color32 ColorFunc(int index, Color32 col)
	            {
	                int step = Mathf.FloorToInt(Now * Mathf.Max(0.01f, speed));
	                float r = (Hash(step, index, 17) % 1000) / 999f;
	                float blink = r < Mathf.Clamp01(onProbability) ? 1f : 0f;

	                Color baseCol = col;
	                Color.RGBToHSV(baseCol, out float h, out float s, out float v);
	                h = Mathf.Repeat(h + (r - 0.5f) * 2f * hueJitter, 1f);
	                s = Mathf.Clamp01(s + saturationBoost * blink);
	                v = Mathf.Clamp01(v + valueBoost * blink);

	                var hsv = Color.HSVToRGB(h, s, v);
	                var result = Color.Lerp(baseCol, hsv, blink);
	                result *= Mathf.Lerp(1f, Mathf.Max(1f, brightness), blink);
	                // Blink by briefly dimming / fading the character.
	                result.a = baseCol.a * Mathf.Lerp(1f, Mathf.Clamp01(minAlpha), blink);
	                return (Color32)result;
	            }

	            public override Matrix4x4 TransFunc(int index)
	            {
	                int step = Mathf.FloorToInt(Now * Mathf.Max(0.01f, speed));
	                float r = (Hash(step, index, 23) % 1000) / 999f;
	                float blink = r < Mathf.Clamp01(onProbability) ? 1f : 0f;

	                float s = Mathf.Lerp(1f, Mathf.Max(0.01f, minScale), blink);
	                float y = Mathf.Lerp(0f, Mathf.Max(0f, yOffset), blink);
	                return Matrix4x4.TRS(new Vector3(0f, y, 0f), Quaternion.identity, Vector3.one * s);
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

	        public class FloatEffect : BaseEffect
	        {
	            public float speed = 2.2f;
	            public float indexStep = 0.35f;
	            public float amplitude = 0.18f;
	            public Vector3 scale = new Vector3(0.03f, 0.03f, 0f);
	            public bool useUnscaledTime = true;

	            private float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

	            public override Matrix4x4 TransFunc(int index)
	            {
	                float t = Mathf.Sin(Now * Mathf.Max(0.01f, speed) + (index - CurrentStart) * indexStep);
	                return Matrix4x4.TRS(Vector3.up * (t * amplitude), Quaternion.identity, Vector3.one + scale * t);
	            }
	        }

	        public class ImpactEffect : BaseEffect
	        {
	            public float duration = 0.55f;
	            public float frequency = 9f;
	            public float amplitude = 0.25f;
	            public float yAmplitude = 0.2f;
	            public float damping = 9f;
	            public bool useUnscaledTime = true;

	            private float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

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

	            private readonly Dictionary<LinkKey, float> _startTimeByLink = new Dictionary<LinkKey, float>();
	            private readonly Dictionary<LinkKey, float> _lastSeenTimeByLink = new Dictionary<LinkKey, float>();
	            private readonly Dictionary<int, string> _lastTextByContext = new Dictionary<int, string>();

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
	                    if (now + 0.01f < startTime)
	                        startTime = now;
	                    if (_lastSeenTimeByLink.TryGetValue(key, out var lastSeen) && now - lastSeen > 0.5f)
	                        startTime = now;
	                }

	                _startTimeByLink[key] = startTime;
	                _lastSeenTimeByLink[key] = now;

	                MeshVertUpdateLoop(context, start, length, TransFunc, ColorFunc, mode);
	            }

	            public override Matrix4x4 TransFunc(int index)
	            {
	                float elapsed = GetElapsed();
	                if (elapsed <= 0f)
	                    return Matrix4x4.identity;

	                if (elapsed >= Mathf.Max(0.01f, duration))
	                    return Matrix4x4.identity;

	                float d = Mathf.Max(0.0001f, damping);
	                float decay = Mathf.Exp(-d * elapsed);
	                float w = Mathf.Max(0.01f, frequency) * Mathf.PI * 2f;
	                float s = 1f + amplitude * decay * Mathf.Sin(w * elapsed);
	                float y = yAmplitude * decay * Mathf.Sin(w * elapsed);

	                return Matrix4x4.TRS(new Vector3(0, y, 0), Quaternion.identity, Vector3.one * Mathf.Max(0.01f, s));
	            }

	            private float GetElapsed()
	            {
	                int ctxId = CurrentContext != null ? CurrentContext.GetInstanceID() : 0;
	                var key = new LinkKey(ctxId, CurrentStart, CurrentLength);
	                if (!_startTimeByLink.TryGetValue(key, out var startTime))
	                    return 0f;
	                return Mathf.Max(0f, Now - startTime);
	            }

	            private void ResetContext(int ctxId)
	            {
	                if (_startTimeByLink.Count == 0)
	                    return;

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
	            }
	        }

	        public class RevealEffect : BaseEffect
	        {
	            public float duration = 0.7f;
	            public float feather01 = 0.06f;
	            public bool useUnscaledTime = true;

	            private float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

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

	            private struct RevealSettings
	            {
	                public float duration;
	                public float feather01;
	            }

	            private readonly Dictionary<LinkKey, float> _startTimeByLink = new Dictionary<LinkKey, float>();
	            private readonly Dictionary<LinkKey, float> _lastSeenTimeByLink = new Dictionary<LinkKey, float>();
	            private readonly Dictionary<LinkKey, RevealSettings> _settingsByLink = new Dictionary<LinkKey, RevealSettings>();
	            private readonly Dictionary<int, string> _lastTextByContext = new Dictionary<int, string>();

	            private float _minX;
	            private float _maxX;

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
	                    if (now + 0.01f < startTime)
	                        startTime = now;
	                    if (_lastSeenTimeByLink.TryGetValue(key, out var lastSeen) && now - lastSeen > 0.5f)
	                        startTime = now;
	                }

	                _startTimeByLink[key] = startTime;
	                _lastSeenTimeByLink[key] = now;

	                UpdateSettingsFromLinkId(context, key);
	                if (context != null && context.TMProComponent != null)
	                    ComputeMinMaxX(context.TMProComponent.textInfo, start, length, out _minX, out _maxX);

	                MeshVertUpdateLoop(context, start, length, TransFunc, ColorFunc, mode);
	            }

	            public override Color32 ColorFunc(int index, Color32 col)
	            {
	                float vis = GetVisibility01(index);
	                if (vis >= 0.999f)
	                    return col;

	                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(col.a * vis), 0, 255);
	                return new Color32(col.r, col.g, col.b, a);
	            }

	            public override Matrix4x4 TransFunc(int index)
	            {
	                float vis = GetVisibility01(index);
	                if (vis <= 0.001f)
	                    return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.zero);
	                return Matrix4x4.identity;
	            }

	            private float GetVisibility01(int index)
	            {
	                if (CurrentContext == null || CurrentContext.TMProComponent == null)
	                    return 1f;

	                var textInfo = CurrentContext.TMProComponent.textInfo;
	                if (textInfo == null || textInfo.characterInfo == null || (uint)index >= (uint)textInfo.characterCount)
	                    return 1f;

	                float range = Mathf.Max(0.0001f, _maxX - _minX);
	                float x = (textInfo.characterInfo[index].bottomLeft.x + textInfo.characterInfo[index].topRight.x) * 0.5f;

	                TryGetSettings(out var settings);
	                float dur = Mathf.Max(0.01f, settings.duration);
	                float t = Mathf.Clamp01(GetElapsed() / dur);
	                float thresholdX = _minX + range * t;

	                float feather = Mathf.Clamp01(settings.feather01) * range;
	                if (feather <= 0.0001f)
	                    return x <= thresholdX ? 1f : 0f;

	                return Mathf.Clamp01(1f - Mathf.InverseLerp(thresholdX - feather, thresholdX + feather, x));
	            }

	            private float GetElapsed()
	            {
	                int ctxId = CurrentContext != null ? CurrentContext.GetInstanceID() : 0;
	                var key = new LinkKey(ctxId, CurrentStart, CurrentLength);
	                if (!_startTimeByLink.TryGetValue(key, out var startTime))
	                    return 0f;
	                return Mathf.Max(0f, Now - startTime);
	            }

	            private bool TryGetSettings(out RevealSettings settings)
	            {
	                int ctxId = CurrentContext != null ? CurrentContext.GetInstanceID() : 0;
	                var key = new LinkKey(ctxId, CurrentStart, CurrentLength);

	                if (_settingsByLink.TryGetValue(key, out settings))
	                    return true;

	                settings = new RevealSettings
	                {
	                    duration = duration,
	                    feather01 = feather01
	                };
	                return false;
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

	                if (!TryParseRevealParams(linkId, out var parsed))
	                    return;

	                _settingsByLink[key] = parsed;
	            }

	            private bool TryParseRevealParams(string linkIdRaw, out RevealSettings settings)
	            {
	                settings = new RevealSettings
	                {
	                    duration = duration,
	                    feather01 = feather01
	                };

	                if (string.IsNullOrEmpty(linkIdRaw))
	                    return false;

	                string linkId = linkIdRaw.Trim();
	                if (linkId.Length >= 2)
	                {
	                    char first = linkId[0];
	                    char last = linkId[linkId.Length - 1];
	                    if ((first == '\"' && last == '\"') || (first == '\'' && last == '\''))
	                        linkId = linkId.Substring(1, linkId.Length - 2);
	                }

	                int sep = linkId.IndexOfAny(new[] { ':', '=', '(' });
	                if (sep < 0)
	                    return false;

	                string prefix = linkId.Substring(0, sep).Trim();
	                if (!string.Equals(prefix, "reveal", StringComparison.OrdinalIgnoreCase))
	                    return false;

	                string paramPart = linkId.Substring(sep + 1).Trim();
	                if (paramPart.EndsWith(")"))
	                    paramPart = paramPart.Substring(0, paramPart.Length - 1).Trim();
	                if (string.IsNullOrEmpty(paramPart))
	                    return false;

	                string[] parts = paramPart.Split(',');
	                if (parts.Length >= 1 && float.TryParse(parts[0].Trim(), out var d))
	                    settings.duration = d;
	                if (parts.Length >= 2 && float.TryParse(parts[1].Trim(), out var f))
	                    settings.feather01 = f;

	                return true;
	            }

	            private static void ComputeMinMaxX(TMP_TextInfo textInfo, int start, int length, out float minX, out float maxX)
	            {
	                minX = 0f;
	                maxX = 0f;
	                if (textInfo == null || textInfo.characterInfo == null || textInfo.characterCount == 0)
	                    return;

	                int end = start + length;
	                int safeStart = Mathf.Clamp(start, 0, textInfo.characterCount);
	                int safeEnd = Mathf.Clamp(end, 0, textInfo.characterCount);

	                bool hasAny = false;
	                for (int i = safeStart; i < safeEnd; i++)
	                {
	                    var ci = textInfo.characterInfo[i];
	                    if (!ci.isVisible) continue;

	                    if (!hasAny)
	                    {
	                        minX = ci.bottomLeft.x;
	                        maxX = ci.topRight.x;
	                        hasAny = true;
	                    }
	                    else
	                    {
	                        minX = Mathf.Min(minX, ci.bottomLeft.x);
	                        maxX = Mathf.Max(maxX, ci.topRight.x);
	                    }
	                }
	            }

	            private void ResetContext(int ctxId)
	            {
	                if (_startTimeByLink.Count == 0)
	                    return;

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
	        }

	            public class DecodeEffect : BaseEffect
	            {
	            public float duration = 0.8f;
	            public float charactersPerSecond = 8f;
	            public float randomChangeSpeed = 25f;
	            public bool forceMeshUpdateAfterTextChange = true;
	            public bool useUnscaledTime = true;
	            public bool useFontCharacterPool = false;
	            public bool useSourceCharacterPool = true;
	            public string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+[]{};:,.?/\\\\|~";

	            private float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

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

	            private readonly Dictionary<LinkKey, string> _sourceTextByLink = new Dictionary<LinkKey, string>();
	            private readonly Dictionary<int, string> _lastAppliedByContext = new Dictionary<int, string>();
	            private readonly Dictionary<LinkKey, int[]> _positionsByLink = new Dictionary<LinkKey, int[]>();
	            private readonly Dictionary<LinkKey, float> _startTimeByLink = new Dictionary<LinkKey, float>();
	            private readonly Dictionary<LinkKey, int> _lastStepByLink = new Dictionary<LinkKey, int>();
	            private readonly Dictionary<LinkKey, int> _lastRevealByLink = new Dictionary<LinkKey, int>();
	            private readonly HashSet<LinkKey> _doneByLink = new HashSet<LinkKey>();

	            private readonly HashSet<int> _warnedExternalOverwrite = new HashSet<int>();
	            private readonly HashSet<int> _activeDecodeByContext = new HashSet<int>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
	            private readonly Dictionary<LinkKey, float> _lastDebugLogTimeByLink = new Dictionary<LinkKey, float>();
#endif

	            public override void DoEffect(TMProLinkAnimator context, int start, int length)
	            {
	                CurrentContext = context;
	                CurrentStart = start;
	                CurrentLength = length;

	                if (context == null || context.TMProComponent == null)
	                    return;

	                var tmproComponent = context.TMProComponent;
	                char[] pool = GetSafeCharsetPool();
	                if (pool.Length == 0)
	                    return;

	                int ctxId = tmproComponent.GetInstanceID();
	                var linkKey = new LinkKey(ctxId, start, length);

	                string currentText = tmproComponent.text ?? string.Empty;

	                bool isActiveOrDone = _startTimeByLink.ContainsKey(linkKey) || _doneByLink.Contains(linkKey);
	                if (!_sourceTextByLink.TryGetValue(linkKey, out var sourceText))
	                {
	                    sourceText = currentText;
	                    _sourceTextByLink[linkKey] = sourceText;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
	                    Debug.Log($"[DecodeEffect] init ctxId={ctxId} start={start} len={length} sourceLen={sourceText.Length} sourceHash={TMPro.TMP_TextUtilities.GetSimpleHashCode(sourceText)}");
#endif
	                }
	                else if (!isActiveOrDone && !string.Equals(sourceText, currentText, StringComparison.Ordinal))
	                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
	                    if (!_warnedExternalOverwrite.Contains(ctxId))
	                    {
	                        _warnedExternalOverwrite.Add(ctxId);
	                        Debug.LogWarning($"[DecodeEffect] Text changed for TMP instance {ctxId}. Decode will restart from new source text.");
	                    }
#endif
	                    sourceText = currentText;
	                    _sourceTextByLink[linkKey] = sourceText;
	                    ResetContext(ctxId);
	                }
	                else if (_doneByLink.Contains(linkKey) && !string.Equals(currentText, sourceText, StringComparison.Ordinal))
	                {
	                    // Text changed after decode finished: treat as new content and allow decode to run again.
	                    _doneByLink.Remove(linkKey);
	                    sourceText = currentText;
	                    _sourceTextByLink[linkKey] = sourceText;
	                    ResetContext(ctxId);
	                }

	                if (!_lastAppliedByContext.ContainsKey(ctxId))
	                    _lastAppliedByContext[ctxId] = currentText;

	                var textInfo = tmproComponent.textInfo;
	                if (textInfo == null || textInfo.characterInfo == null || textInfo.characterCount == 0)
	                    return;

	                if (_doneByLink.Contains(linkKey))
	                {
	                    // One-shot effect: once done, avoid constantly rewriting TMP.text (which would fight other link effects).
	                    // Only restore our own character range if something changed it.
	                    int[] donePositions = GetOrBuildPositions(linkKey, textInfo, sourceText, start, length);
	                    if (donePositions == null || donePositions.Length == 0)
	                        return;

	                    // If TMP normalized the string length or tags, we can't safely patch by index anymore; restart.
	                    if (currentText.Length != sourceText.Length)
	                    {
	                        _doneByLink.Remove(linkKey);
	                        sourceText = currentText;
	                        _sourceTextByLink[linkKey] = sourceText;
	                        ResetContext(ctxId);
	                        return;
	                    }

	                    bool anyDiff = false;
	                    for (int i = 0; i < donePositions.Length; i++)
	                    {
	                        int p = donePositions[i];
	                        if ((uint)p >= (uint)currentText.Length || (uint)p >= (uint)sourceText.Length)
	                            continue;
	                        if (currentText[p] != sourceText[p])
	                        {
	                            anyDiff = true;
	                            break;
	                        }
	                    }

	                    if (!anyDiff)
	                        return;

	                    char[] fixedChars = currentText.ToCharArray();
	                    for (int i = 0; i < donePositions.Length; i++)
	                    {
	                        int p = donePositions[i];
	                        if ((uint)p >= (uint)fixedChars.Length || (uint)p >= (uint)sourceText.Length)
	                            continue;
	                        fixedChars[p] = sourceText[p];
	                    }

	                    string fixedText = new string(fixedChars);
	                    try
	                    {
	                        Fungus.TMProLinkAnimator.BeginSuppressTextChangedEvent();
	                        tmproComponent.text = fixedText;
	                        if (forceMeshUpdateAfterTextChange)
	                            tmproComponent.ForceMeshUpdate();
	                    }
	                    finally
	                    {
	                        Fungus.TMProLinkAnimator.EndSuppressTextChangedEvent();
	                    }

	                    _lastAppliedByContext[ctxId] = fixedText;
	                    context.SetDirty();
	                    return;
	                }

	                float now = Now;
	                if (!_startTimeByLink.TryGetValue(linkKey, out var startTime))
	                {
	                    startTime = now;
	                    _startTimeByLink[linkKey] = startTime;
	                }

	                int[] positions = GetOrBuildPositions(linkKey, textInfo, sourceText, start, length);
	                if (positions == null || positions.Length == 0)
	                    return;

	                float dur = Mathf.Max(0.01f, duration);
	                float elapsed = now - startTime;
	                float p01 = Mathf.Clamp01(elapsed / dur);
	                int revealCount = Mathf.FloorToInt(p01 * positions.Length);

	                // Optional: discrete stepping (like "every interval replace one more char")
	                if (charactersPerSecond > 0f)
	                {
	                    int stepped = Mathf.FloorToInt(elapsed * charactersPerSecond);
	                    revealCount = Mathf.Clamp(stepped, 0, positions.Length);
	                }

	                bool isDone = revealCount >= positions.Length;
	                if (!isDone)
	                    _activeDecodeByContext.Add(ctxId);

	                int step = Mathf.FloorToInt(now * Mathf.Max(0.01f, randomChangeSpeed));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
	                if (!_lastDebugLogTimeByLink.TryGetValue(linkKey, out var lastLogTime) || now - lastLogTime > 1.0f)
	                {
	                    _lastDebugLogTimeByLink[linkKey] = now;
	                    Debug.Log($"[DecodeEffect] tick ctxId={ctxId} start={start} len={length} now={now:F2} elapsed={elapsed:F2} step={step} reveal={revealCount}/{positions.Length} curTextLen={currentText.Length}");
	                }
#endif
	                if (_lastStepByLink.TryGetValue(linkKey, out var lastStep) &&
	                    _lastRevealByLink.TryGetValue(linkKey, out var lastReveal) &&
	                    lastStep == step && lastReveal == revealCount)
	                {
	                    return;
	                }

	                // Use currentText as the working buffer so other (non-overlapping) link effects can coexist.
	                // Fall back to sourceText if TMP normalized the raw string length.
	                string workingText = currentText;
	                if (workingText.Length != sourceText.Length)
	                    workingText = sourceText;

	                char[] chars = workingText.ToCharArray();

	                // Make a garbled version first, then progressively restore original characters.
	                for (int i = 0; i < positions.Length; i++)
	                {
	                    int p = positions[i];
	                    if ((uint)p >= (uint)chars.Length)
	                        continue;
	                    if (i < revealCount)
	                    {
	                        if ((uint)p < (uint)sourceText.Length)
	                            chars[p] = sourceText[p];
	                        continue;
	                    }
	                    chars[p] = pool[Hash(step, p, i) % pool.Length];
	                }

	                string decoded = new string(chars);

	                try
	                {
	                    Fungus.TMProLinkAnimator.BeginSuppressTextChangedEvent();
	                    tmproComponent.text = decoded;
	                    if (forceMeshUpdateAfterTextChange)
	                        tmproComponent.ForceMeshUpdate();
	                }
	                finally
	                {
	                    Fungus.TMProLinkAnimator.EndSuppressTextChangedEvent();
	                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
	                if (revealCount == 0)
	                {
	                    Debug.Log($"[DecodeEffect] applied-start ctxId={ctxId} step={step} workingHash={TMPro.TMP_TextUtilities.GetSimpleHashCode(workingText)} decodedHash={TMPro.TMP_TextUtilities.GetSimpleHashCode(decoded)}");
	                }
#endif

	                _lastAppliedByContext[ctxId] = decoded;
	                _lastStepByLink[linkKey] = step;
	                _lastRevealByLink[linkKey] = revealCount;
	                context.SetDirty();

	                if (isDone)
	                {
	                    _activeDecodeByContext.Remove(ctxId);
	                    _doneByLink.Add(linkKey);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
	                    Debug.Log($"[DecodeEffect] done ctxId={ctxId} start={start} len={length} sourceLen={sourceText.Length} sourceHash={TMPro.TMP_TextUtilities.GetSimpleHashCode(sourceText)}");
#endif
	                }
	            }

	            private void ResetContext(int ctxId)
	            {
	                if (_startTimeByLink.Count == 0)
	                    return;

	                var toRemove = new List<LinkKey>();
	                foreach (var kv in _startTimeByLink)
	                {
	                    if (kv.Key.contextId == ctxId)
	                        toRemove.Add(kv.Key);
	                }
	                for (int i = 0; i < toRemove.Count; i++)
	                {
	                    _sourceTextByLink.Remove(toRemove[i]);
	                    _startTimeByLink.Remove(toRemove[i]);
	                    _lastStepByLink.Remove(toRemove[i]);
	                    _lastRevealByLink.Remove(toRemove[i]);
	                    _positionsByLink.Remove(toRemove[i]);
	                    _doneByLink.Remove(toRemove[i]);
	                }
	            }

	            private static char[] GetSafeCharsetPool()
	            {
	                // Use a static filtered pool so we never insert characters that TMP parses as tags.
	                const string defaultCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^*()-_=+[]{};:,.?/\\\\|~";
	                var raw = defaultCharset.ToCharArray();
	                var list = new List<char>(raw.Length);
	                for (int i = 0; i < raw.Length; i++)
	                {
	                    char c = raw[i];
	                    if (char.IsControl(c) || char.IsWhiteSpace(c))
	                        continue;
	                    if (c == '<' || c == '>' || c == '&')
	                        continue;
	                    list.Add(c);
	                }
	                return list.ToArray();
	            }

	            private int[] GetOrBuildPositions(LinkKey linkKey, TMP_TextInfo textInfo, string baseText, int start, int length)
	            {
	                if (_positionsByLink.TryGetValue(linkKey, out var cached) && cached != null && cached.Length > 0)
	                    return cached;

	                if (textInfo == null || textInfo.characterInfo == null || textInfo.characterCount == 0)
	                    return null;
	                if (string.IsNullOrEmpty(baseText))
	                    return null;

	                int end = start + length;
	                int safeStart = Mathf.Clamp(start, 0, textInfo.characterCount);
	                int safeEnd = Mathf.Clamp(end, 0, textInfo.characterCount);

	                var list = new List<int>(Mathf.Min(64, length));
	                for (int i = safeStart; i < safeEnd; i++)
	                {
	                    var ci = textInfo.characterInfo[i];
	                    if (!ci.isVisible) continue;

	                    int pos = ci.index;
	                    int len = Mathf.Max(1, ci.stringLength);
	                    for (int j = 0; j < len; j++)
	                    {
	                        int p = pos + j;
	                        if ((uint)p >= (uint)baseText.Length) continue;
	                        char c = baseText[p];
	                        if (char.IsControl(c) || char.IsWhiteSpace(c)) continue;
	                        if (c == '<' || c == '>' || c == '&') continue;
	                        list.Add(p);
	                    }
	                }

	                // De-dupe and stabilize order
	                if (list.Count == 0)
	                {
	                    _positionsByLink[linkKey] = Array.Empty<int>();
	                    return _positionsByLink[linkKey];
	                }

	                list.Sort();
	                int write = 1;
	                for (int read = 1; read < list.Count; read++)
	                {
	                    if (list[read] != list[write - 1])
	                        list[write++] = list[read];
	                }
	                if (write != list.Count)
	                    list.RemoveRange(write, list.Count - write);

	                var arr = list.ToArray();
	                _positionsByLink[linkKey] = arr;
	                return arr;
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
