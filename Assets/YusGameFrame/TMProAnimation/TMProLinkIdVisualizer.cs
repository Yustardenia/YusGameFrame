using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace YusGameFrame
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class TMProLinkIdVisualizer : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;
        [SerializeField] private bool refreshInEditMode = true;

        public TMP_Text Target
        {
            get => target;
            set => target = value;
        }

        public bool RefreshInEditMode
        {
            get => refreshInEditMode;
            set => refreshInEditMode = value;
        }

        [Serializable]
        public struct LinkData
        {
            public int index;
            public int firstCharacterIndex;
            public int characterLength;
            public string linkText;
            public string linkIdRaw;
            public string linkIdSanitized;
            public string effectKey;
            public string paramPart;
            public string[] orderedParams;
            public KeyValuePair<string, string>[] namedParams;
        }

        private readonly List<LinkData> _cache = new List<LinkData>();
        private int _lastTextHash;
        private int _lastLinkCount;

        public IReadOnlyList<LinkData> CachedLinks => _cache;

        private void OnEnable()
        {
            if (target == null)
                target = GetComponent<TMP_Text>();

            RebuildCache(forceMeshUpdate: true);
        }

        private void OnValidate()
        {
            if (target == null)
                target = GetComponent<TMP_Text>();

            if (!Application.isPlaying && !refreshInEditMode)
                return;

            RebuildCache(forceMeshUpdate: true);
        }

        private void Update()
        {
            if (!Application.isPlaying && !refreshInEditMode)
                return;

            RebuildCache(forceMeshUpdate: false);
        }

        public void RebuildCache(bool forceMeshUpdate)
        {
            if (target == null)
                return;

            if (!Application.isPlaying && !refreshInEditMode)
                return;

            if (forceMeshUpdate)
                target.ForceMeshUpdate();

            var textInfo = target.textInfo;
            if (textInfo == null)
                return;

            int textHash = target.text != null ? target.text.GetHashCode() : 0;
            int linkCount = textInfo.linkCount;

            if (!forceMeshUpdate && textHash == _lastTextHash && linkCount == _lastLinkCount)
                return;

            _lastTextHash = textHash;
            _lastLinkCount = linkCount;

            _cache.Clear();
            for (int i = 0; i < linkCount; i++)
            {
                var li = textInfo.linkInfo[i];
                string idRaw = li.GetLinkID();
                string idSanitized = SanitizeLinkId(idRaw);
                string effectKey = ExtractEffectKey(idSanitized, out var sepIndex, out var sepChar);
                string paramPart = ExtractParamPart(idSanitized, sepIndex, sepChar);
                ParseParams(paramPart, out var ordered, out var named);

                _cache.Add(new LinkData
                {
                    index = i,
                    firstCharacterIndex = li.linkTextfirstCharacterIndex,
                    characterLength = li.linkTextLength,
                    linkText = li.GetLinkText(),
                    linkIdRaw = idRaw,
                    linkIdSanitized = idSanitized,
                    effectKey = effectKey,
                    paramPart = paramPart,
                    orderedParams = ordered,
                    namedParams = named
                });
            }
        }

        public static string SanitizeLinkId(string linkId)
        {
            if (string.IsNullOrEmpty(linkId))
                return linkId;

            linkId = linkId.Trim();
            if (linkId.Length < 2)
                return linkId;

            char first = linkId[0];
            char last = linkId[linkId.Length - 1];
            if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                return linkId.Substring(1, linkId.Length - 2);

            return linkId;
        }

        public static string ExtractEffectKey(string linkId, out int sepIndex, out char sepChar)
        {
            sepIndex = -1;
            sepChar = '\0';

            if (string.IsNullOrEmpty(linkId))
                return linkId;

            sepIndex = linkId.IndexOfAny(new[] { ':', '=', '(', ',', '?', '&', ';' });
            if (sepIndex <= 0)
            {
                sepIndex = -1;
                return linkId.Trim();
            }

            sepChar = linkId[sepIndex];
            return linkId.Substring(0, sepIndex).Trim();
        }

        private static string ExtractParamPart(string linkId, int sepIndex, char sepChar)
        {
            if (string.IsNullOrEmpty(linkId))
                return string.Empty;

            if (sepIndex < 0 || sepIndex >= linkId.Length - 1)
                return string.Empty;

            string paramPart = linkId.Substring(sepIndex + 1).Trim();
            if (sepChar == '(' && paramPart.EndsWith(")", StringComparison.Ordinal))
                paramPart = paramPart.Substring(0, paramPart.Length - 1).Trim();

            return paramPart;
        }

        private static void ParseParams(string paramPart, out string[] orderedParams, out KeyValuePair<string, string>[] namedParams)
        {
            orderedParams = Array.Empty<string>();
            namedParams = Array.Empty<KeyValuePair<string, string>>();

            if (string.IsNullOrWhiteSpace(paramPart))
                return;

            string part = paramPart.Trim();

            if (part.Contains("="))
            {
                var pairs = new List<KeyValuePair<string, string>>();
                string[] tokens = part.Split(new[] { '&', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i].Trim();
                    int eq = token.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    string k = token.Substring(0, eq).Trim();
                    string v = token.Substring(eq + 1).Trim();
                    if (k.Length == 0)
                        continue;

                    pairs.Add(new KeyValuePair<string, string>(k, v));
                }
                namedParams = pairs.ToArray();
                return;
            }

            if (part.Contains(","))
            {
                string[] tokens = part.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tokens.Length; i++)
                    tokens[i] = tokens[i].Trim();

                orderedParams = tokens;
                return;
            }

            orderedParams = new[] { part };
        }
    }
}

