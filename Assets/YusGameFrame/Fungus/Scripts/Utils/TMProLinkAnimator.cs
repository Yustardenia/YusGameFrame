// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
#if UNITY_2018_1_OR_NEWER
namespace Fungus
{
/// <summary>
/// Component that is automatically added to all tmpro texts that contain links. Caches
/// local data for that TMProText and uses the TMProLinkAnimLookup to the actual animation.
/// </summary>
[DisallowMultipleComponent]
public class TMProLinkAnimator : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const int MaxMissingLinkLogs = 30;
    private static int _missingLinkLogCount;
    private static readonly HashSet<string> _missingLinkLogDedup = new HashSet<string>();

    private const int MaxResolvedLinkLogs = 30;
    private static int _resolvedLinkLogCount;
    private static readonly HashSet<string> _resolvedLinkLogDedup = new HashSet<string>();
#endif

#region Auto Add Component
    // When link effects mutate TMP text at runtime, TMP raises TEXT_CHANGED_EVENT which can re-enter
    // the auto-add / update pipeline and cause infinite recursion. Use this guard to suppress it.
    private static int _suppressTextChangedEventDepth;

    public static void BeginSuppressTextChangedEvent()
    {
        _suppressTextChangedEventDepth++;
    }

    public static void EndSuppressTextChangedEvent()
    {
        if (_suppressTextChangedEventDepth > 0)
            _suppressTextChangedEventDepth--;
    }

    /// <summary>
    /// Ensure we are being notified of TMPro changes.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void RegisterAutoAddTMPLinkAnim()
    {
        // When domain reload is disabled, this method can be called multiple times across play sessions.
        // Ensure the subscription is idempotent.
        TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(AutoAddTMPLinkAnim);
        TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Add(AutoAddTMPLinkAnim);
    }

    /// <summary>
    /// Fallback pass for texts that already exist at scene load but don't trigger TEXT_CHANGED_EVENT
    /// in some setups. Also registers for future scene loads.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoAddTMPLinkAnimForLoadedScenes()
    {
        if (!Application.isPlaying)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        EnsureAnimatorsInAllLoadedScenes();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Application.isPlaying)
            return;

        EnsureAnimatorsInAllLoadedScenes();
    }

    private static void EnsureAnimatorsInAllLoadedScenes()
    {
        var tmps = FindAllTMPTexts();
        for (int i = 0; i < tmps.Length; i++)
        {
            var tmp = tmps[i];
            if (tmp == null)
                continue;

            // Ensure linkInfo is populated.
            tmp.ForceMeshUpdate();
            if (tmp.textInfo.linkCount <= 0)
                continue;

            EnsureAnimator(tmp);
        }
    }

    private static TMPro.TMP_Text[] FindAllTMPTexts()
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#elif UNITY_2020_1_OR_NEWER
        return Object.FindObjectsOfType<TMPro.TMP_Text>(true);
#else
        return Object.FindObjectsOfType<TMPro.TMP_Text>();
#endif
    }

    private static void EnsureAnimator(TMPro.TMP_Text tmp)
    {
        if (tmp == null)
            return;

        var tmpa = tmp.GetComponent<TMProLinkAnimator>();
        if (tmpa == null)
        {
            tmpa = tmp.gameObject.AddComponent<TMProLinkAnimator>();
            tmpa.TMProComponent = tmp;
        }

        tmpa.SetDirty();
        tmpa.UpdateAnimation();
    }

    /// <summary>
    /// Adds a suite of default link text animations. These can be removed via the
    /// TMProLinkAnimLookup if desired.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void RegisterDefaultTMPLinkAnims()
    {
        TMProLinkAnimLookup.AddHelper("jitter", new TMProLinkAnimEffects.ShakeEffect()
        {
            mode = TMProLinkAnimEffects.TMPLinkAnimatorMode.PerCharacter,
            offsetScale = new Vector2(1, 4),
            rotScale = 10
        });
        TMProLinkAnimLookup.AddHelper("angry", new TMProLinkAnimEffects.ShakeEffect()
        {
            mode = TMProLinkAnimEffects.TMPLinkAnimatorMode.PerWord,
            offsetScale = new Vector2(1, 8),
            rotScale = 4
        });
        TMProLinkAnimLookup.AddHelper("spooky", new TMProLinkAnimEffects.WiggleEffect()
        {
            mode = TMProLinkAnimEffects.TMPLinkAnimatorMode.PerSection,
            offsetScale = new Vector2(6, 10),
            speed = 1.5f,
        });
        TMProLinkAnimLookup.AddHelper("unknowable", new TMProLinkAnimEffects.WiggleEffect()
        {
            mode = TMProLinkAnimEffects.TMPLinkAnimatorMode.PerCharacter,
            offsetScale = new Vector2(4, 8),
            speed = 1f,
        });
        TMProLinkAnimLookup.AddHelper("wave", new TMProLinkAnimEffects.WaveEffect()
        {
            mode = TMProLinkAnimEffects.TMPLinkAnimatorMode.PerCharacter,
            speed = 10,
            indexStep = 0.3f,
            scale = 2
        });
        TMProLinkAnimLookup.AddHelper("swing", new TMProLinkAnimEffects.PivotEffect()
        {
            mode = TMProLinkAnimEffects.TMPLinkAnimatorMode.PerWord,
            speed = 10,
            degScale = 15
        });
        TMProLinkAnimLookup.AddHelper("bounce", new TMProLinkAnimEffects.BounceEffect()
        {
            mode = TMProLinkAnimEffects.TMPLinkAnimatorMode.PerWord,
            speed = 4,
            scale = 5,
        });
        TMProLinkAnimLookup.AddHelper("excited", new TMProLinkAnimEffects.BounceEffect()
        {
            mode = TMProLinkAnimEffects.TMPLinkAnimatorMode.PerCharacter,
            speed = 7,
            scale = 2,
            indexStep = 11.0f / 3.0f,
        });
        TMProLinkAnimLookup.AddHelper("glow", new TMProLinkAnimEffects.PulseEffect()
        {
            mode = TMProLinkAnimEffects.TMPLinkAnimatorMode.PerWord,
            speed = 4,
            HSVIntensityScale = 0.15f,
            hueScale = 0,
            saturationScale = 0.1f,
            scale = new Vector3(0.06f, 0.06f, 0),
        });
    }

    /// <summary>
    /// Called by TMPro when a text is changed, ensuring link animator is there and
    /// that data is ready for it to use.
    /// </summary>
    /// <param name="obj"></param>
    public static void AutoAddTMPLinkAnim(object obj)
    {
        if (Application.isPlaying)
        {
            if (_suppressTextChangedEventDepth > 0)
                return;

            var tmp = (obj as TMPro.TMP_Text);
            if (forcedUpdater == null && tmp.textInfo.linkCount > 0)
            {
                EnsureAnimator(tmp);
            }
        }
    }

    /// <summary>
    /// Cache of the TMProLinkAnimator that just forced an update of the TMProText, used to
    /// prevent cyclic updates of TMPro mesh content.
    /// </summary>
    protected static TMProLinkAnimator forcedUpdater;

    #endregion Auto Add Component

    public TMPro.TMP_Text TMProComponent { get; protected set; }
    public bool dirty = true;
    protected bool needsToForceMeshUpdate = true;

    public TMPro.TMP_MeshInfo[] CachedMeshInfo { get; protected set; }

    public void SetDirty()
    {
        dirty = true;
    }

    protected void Awake()
    {
        if (TMProComponent == null)
        {
            TMProComponent = GetComponent<TMPro.TMP_Text>();
        }
    }

    protected void Update()
    {
        UpdateAnimation();
    }

    /// <summary>
    /// If there is TMPro and a link to potentially animate then ask the AnimLookup for it
    /// </summary>
	    protected void UpdateAnimation()
	    {
	        //could we anim
	        if (TMProComponent != null && enabled)
        {
            bool requiresVertexDataUpdate = false;

            //for all found links
	            for (int i = 0; i < TMProComponent.textInfo.linkCount; i++)
	            {
	                var curLink = TMProComponent.textInfo.linkInfo[i];

	                //if a static lookup exists, ask it to run its animation with us as the context
	                var originalHash = curLink.hashCode;
	                var linkHash = originalHash;
	                string resolvedBy = "originalHash";
	                if (!TMProLinkAnimLookup.LinkHashToEffect.TryGetValue(linkHash, out TMProLinkAnimLookup.TMProAnimFunc animFunc))
	                {
	                    // Some TMP versions / parsing paths can produce a hashCode that doesn't match
	                    // TMP_TextUtilities.GetSimpleHashCode(GetLinkID()). Recompute as a fallback.
	                    var linkIdRaw = curLink.GetLinkID();
	                    var linkId = SanitizeLinkId(linkIdRaw);
	                    int fullIdHash = 0;
	                    int keyHash = 0;
	                    string effectKey = null;
	                    if (!string.IsNullOrEmpty(linkId))
	                    {
	                        // Try full ID first (supports exact matches).
	                        fullIdHash = TMPro.TMP_TextUtilities.GetSimpleHashCode(linkId);
	                        linkHash = fullIdHash;
	                        resolvedBy = "fullId";
	                        if (!TMProLinkAnimLookup.LinkHashToEffect.TryGetValue(linkHash, out animFunc))
	                        {
	                            // Then try effect key (supports parameterized IDs like "type:8,0.2").
	                            effectKey = ExtractEffectKey(linkId);
	                            if (!string.IsNullOrEmpty(effectKey) && !string.Equals(effectKey, linkId, StringComparison.Ordinal))
	                            {
	                                keyHash = TMPro.TMP_TextUtilities.GetSimpleHashCode(effectKey);
	                                linkHash = keyHash;
	                                resolvedBy = "effectKey";
	                                TMProLinkAnimLookup.LinkHashToEffect.TryGetValue(linkHash, out animFunc);
	                            }
	                        }
	                    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
	                    if (animFunc == null)
	                    {
	                        ReportMissingLink(curLink, linkIdRaw, linkId, effectKey, originalHash, fullIdHash, keyHash);
	                    }
#endif
	                }

	                if (animFunc != null)
	                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
	                    // Confirms that the link was matched to an effect (useful when effects "do nothing" visually).
	                    ReportResolvedLinkOnce(curLink, resolvedBy, originalHash, linkHash);
#endif
                    //only update caches if we actually need it
                    HandleDirty();

                    animFunc(this, curLink.linkTextfirstCharacterIndex, curLink.linkTextLength);

                    requiresVertexDataUpdate = true;
                }
            }

            // Push changes if we actually found a matching effect
            if (requiresVertexDataUpdate)
            {
                TMProComponent.UpdateVertexData();
            }
        }
    }

	    private static string SanitizeLinkId(string linkId)
	    {
	        if (string.IsNullOrEmpty(linkId))
	            return linkId;

	        linkId = linkId.Trim();

	        // In some markup styles the ID can include surrounding quotes.
	        if (linkId.Length >= 2)
	        {
	            char first = linkId[0];
            char last = linkId[linkId.Length - 1];
            if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                return linkId.Substring(1, linkId.Length - 2);
        }

	        return linkId;
	    }

	    private static string ExtractEffectKey(string linkId)
	    {
	        if (string.IsNullOrEmpty(linkId))
	            return linkId;

	        // Supported parameter separators: "type:8", "type=8", "type(8,0.2)", etc.
	        int idx = linkId.IndexOfAny(new[] { ':', '=', '(', ',' });
	        if (idx <= 0)
	            return linkId;

	        return linkId.Substring(0, idx).Trim();
	    }

    protected void HandleDirty()
    {
        //update internal cache if underlying data has changed
        if (dirty)
        {
            if (needsToForceMeshUpdate)
            {
                forcedUpdater = this;
                TMProComponent.ForceMeshUpdate();
                forcedUpdater = null;
            }
            CachedMeshInfo = TMProComponent.textInfo.CopyMeshInfoVertexData();
            dirty = false;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void ReportMissingLink(TMPro.TMP_LinkInfo linkInfo, string linkIdRaw, string linkIdSanitized, string effectKey, int originalHash, int fullIdHash, int keyHash)
    {
        if (_missingLinkLogCount >= MaxMissingLinkLogs)
            return;

        int textId = TMProComponent != null ? TMProComponent.GetInstanceID() : 0;
        string dedupKey = $"{textId}:{linkInfo.linkTextfirstCharacterIndex}:{linkInfo.linkTextLength}:{linkIdRaw}";
        if (!_missingLinkLogDedup.Add(dedupKey))
            return;

        _missingLinkLogCount++;

        string objName = TMProComponent != null ? TMProComponent.gameObject.name : "(null)";
        string sceneName = TMProComponent != null ? TMProComponent.gameObject.scene.name : "(unknown)";

        Debug.LogWarning(
            "[TMProLinkAnimator] Missing link effect mapping.\n" +
            $"  scene={sceneName} obj={objName} tmpId={textId}\n" +
            $"  linkText=\"{linkInfo.GetLinkText()}\"\n" +
            $"  linkIdRaw=\"{linkIdRaw}\" linkIdSanitized=\"{linkIdSanitized}\" effectKey=\"{effectKey}\"\n" +
            $"  hashes: original={originalHash} fullId={fullIdHash} key={keyHash}\n" +
            "  Tip: check CustomTMProEffects registration log and ensure the effect key matches AddHelper(\"...\")."
        );
    }

    private void ReportResolvedLinkOnce(TMPro.TMP_LinkInfo linkInfo, string resolvedBy, int originalHash, int matchedHash)
    {
        if (_resolvedLinkLogCount >= MaxResolvedLinkLogs)
            return;

        int textId = TMProComponent != null ? TMProComponent.GetInstanceID() : 0;
        string linkIdRaw = linkInfo.GetLinkID();
        string dedupKey = $"{textId}:{linkInfo.linkTextfirstCharacterIndex}:{linkInfo.linkTextLength}:{linkIdRaw}:{matchedHash}";
        if (!_resolvedLinkLogDedup.Add(dedupKey))
            return;

        _resolvedLinkLogCount++;

        string objName = TMProComponent != null ? TMProComponent.gameObject.name : "(null)";
        string sceneName = TMProComponent != null ? TMProComponent.gameObject.scene.name : "(unknown)";
        string linkIdSanitized = SanitizeLinkId(linkIdRaw);
        string effectKey = ExtractEffectKey(linkIdSanitized);

        Debug.Log(
            "[TMProLinkAnimator] Resolved link effect mapping.\n" +
            $"  scene={sceneName} obj={objName} tmpId={textId}\n" +
            $"  linkText=\"{linkInfo.GetLinkText()}\"\n" +
            $"  linkIdRaw=\"{linkIdRaw}\" linkIdSanitized=\"{linkIdSanitized}\" effectKey=\"{effectKey}\"\n" +
            $"  resolvedBy={resolvedBy} hashes: original={originalHash} matched={matchedHash}"
        );
    }
#endif
}
}
#endif
