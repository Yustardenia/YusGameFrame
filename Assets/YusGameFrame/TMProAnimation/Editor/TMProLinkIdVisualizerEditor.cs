using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace YusGameFrame.Editor
{
    [CustomEditor(typeof(TMProLinkIdVisualizer))]
    public sealed class TMProLinkIdVisualizerEditor : UnityEditor.Editor
    {
        private static readonly Color DefaultRectColor = new Color(0.2f, 0.75f, 1f, 0.8f);

        private bool drawSceneRects = true;
        private bool drawSceneLabels = true;
        private Color rectColor = DefaultRectColor;

        public override void OnInspectorGUI()
        {
            var visualizer = (TMProLinkIdVisualizer)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("refreshInEditMode"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh"))
                    visualizer.RebuildCache(forceMeshUpdate: true);

                if (GUILayout.Button("ForceMeshUpdate"))
                {
                    if (visualizer.Target != null)
                        visualizer.Target.ForceMeshUpdate();
                    visualizer.RebuildCache(forceMeshUpdate: true);
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                drawSceneRects = EditorGUILayout.ToggleLeft("Scene: Draw Link Rects", drawSceneRects);
                drawSceneLabels = EditorGUILayout.ToggleLeft("Scene: Draw Link Labels", drawSceneLabels);
                rectColor = EditorGUILayout.ColorField("Scene: Color", rectColor);
            }

            serializedObject.ApplyModifiedProperties();

            var links = visualizer.CachedLinks;
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"Links ({links.Count})", EditorStyles.boldLabel);

            if (visualizer.Target == null)
            {
                EditorGUILayout.HelpBox("Assign a TMP_Text target to visualize its <link> IDs/params.", MessageType.Info);
                return;
            }

            if (links.Count == 0)
            {
                EditorGUILayout.HelpBox("No <link> tags found (or TMP has not generated textInfo yet).", MessageType.Info);
                return;
            }

            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField($"#{i}  chars [{link.firstCharacterIndex}, +{link.characterLength}]", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField("Text", link.linkText);
                    EditorGUILayout.LabelField("ID (raw)", link.linkIdRaw);
                    EditorGUILayout.LabelField("ID (sanitized)", link.linkIdSanitized);
                    EditorGUILayout.LabelField("EffectKey", string.IsNullOrEmpty(link.effectKey) ? "(empty)" : link.effectKey);
                    EditorGUILayout.LabelField("Params", string.IsNullOrEmpty(link.paramPart) ? "(none)" : link.paramPart);

                    if (link.namedParams != null && link.namedParams.Length > 0)
                    {
                        EditorGUILayout.LabelField("Named Params", EditorStyles.miniBoldLabel);
                        for (int p = 0; p < link.namedParams.Length; p++)
                            EditorGUILayout.LabelField($"- {link.namedParams[p].Key}", link.namedParams[p].Value);
                    }
                    else if (link.orderedParams != null && link.orderedParams.Length > 0)
                    {
                        EditorGUILayout.LabelField("Ordered Params", EditorStyles.miniBoldLabel);
                        for (int p = 0; p < link.orderedParams.Length; p++)
                            EditorGUILayout.LabelField($"[{p}]", link.orderedParams[p]);
                    }
                }
            }
        }

        private void OnSceneGUI()
        {
            if (!drawSceneRects && !drawSceneLabels)
                return;

            var visualizer = (TMProLinkIdVisualizer)target;
            TMP_Text tmp = visualizer != null ? visualizer.Target : null;
            if (tmp == null)
                return;

            visualizer.RebuildCache(forceMeshUpdate: false);

            var textInfo = tmp.textInfo;
            if (textInfo == null || textInfo.linkCount == 0)
                return;

            Handles.color = rectColor;

            for (int i = 0; i < textInfo.linkCount; i++)
            {
                var linkInfo = textInfo.linkInfo[i];
                var rects = GetLinkRectsLocal(tmp, linkInfo);
                for (int r = 0; r < rects.Count; r++)
                {
                    var rect = rects[r];
                    DrawRectWorld(tmp.transform, rect);

                    if (drawSceneLabels && r == 0)
                    {
                        string id = TMProLinkIdVisualizer.SanitizeLinkId(linkInfo.GetLinkID());
                        string key = TMProLinkIdVisualizer.ExtractEffectKey(id, out _, out _);
                        string label = string.IsNullOrEmpty(key) ? id : key;
                        var pos = tmp.transform.TransformPoint(new Vector3(rect.center.x, rect.yMax, 0));
                        Handles.Label(pos, label, EditorStyles.whiteMiniLabel);
                    }
                }
            }
        }

        private static List<Rect> GetLinkRectsLocal(TMP_Text tmp, TMP_LinkInfo linkInfo)
        {
            var rects = new List<Rect>();
            var textInfo = tmp.textInfo;
            if (textInfo == null || textInfo.characterCount == 0)
                return rects;

            int start = linkInfo.linkTextfirstCharacterIndex;
            int endExclusive = start + linkInfo.linkTextLength;

            int currentLine = -1;
            bool hasAny = false;
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;

            int safeStart = Mathf.Clamp(start, 0, textInfo.characterCount);
            int safeEnd = Mathf.Clamp(endExclusive, 0, textInfo.characterCount);

            for (int i = safeStart; i < safeEnd; i++)
            {
                var ci = textInfo.characterInfo[i];
                if (!ci.isVisible)
                    continue;

                if (!hasAny)
                {
                    currentLine = ci.lineNumber;
                    min = ci.bottomLeft;
                    max = ci.topRight;
                    hasAny = true;
                    continue;
                }

                if (ci.lineNumber != currentLine)
                {
                    rects.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
                    currentLine = ci.lineNumber;
                    min = ci.bottomLeft;
                    max = ci.topRight;
                    continue;
                }

                min = Vector2.Min(min, ci.bottomLeft);
                max = Vector2.Max(max, ci.topRight);
            }

            if (hasAny)
                rects.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));

            return rects;
        }

        private static void DrawRectWorld(Transform t, Rect localRect)
        {
            if (t == null)
                return;

            Vector3 p0 = t.TransformPoint(new Vector3(localRect.xMin, localRect.yMin, 0));
            Vector3 p1 = t.TransformPoint(new Vector3(localRect.xMin, localRect.yMax, 0));
            Vector3 p2 = t.TransformPoint(new Vector3(localRect.xMax, localRect.yMax, 0));
            Vector3 p3 = t.TransformPoint(new Vector3(localRect.xMax, localRect.yMin, 0));

            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p1, p2);
            Handles.DrawLine(p2, p3);
            Handles.DrawLine(p3, p0);
        }
    }
}

