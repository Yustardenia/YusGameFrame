#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class YusBatchChangeFontWindow : EditorWindow
{
    private TMP_FontAsset _tmpFont;
    private Font _textFont;
    private bool _onlyActiveScene = false;
    private bool _includePrefabs = true;
    private Vector2 _scroll;
    private int _cachedTmpCount;
    private int _cachedTextCount;
    private int _cachedPrefabTmpCount;
    private int _cachedPrefabTextCount;
    private bool _hasScan;

    [MenuItem(YusGameFrameEditorMenu.Root + "Tools/UI/一键更改字体")]
    public static void Open()
    {
        GetWindow<YusBatchChangeFontWindow>("一键更改字体");
    }

    private void OnEnable()
    {
        ScanCounts();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("批量统一字体（场景内）", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            _tmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField("TMP/TMPUI 字体", _tmpFont, typeof(TMP_FontAsset), false);
            _textFont = (Font)EditorGUILayout.ObjectField("Text 字体", _textFont, typeof(Font), false);
            EditorGUI.BeginChangeCheck();
            _onlyActiveScene = EditorGUILayout.ToggleLeft("仅处理当前活动场景", _onlyActiveScene);
            if (EditorGUI.EndChangeCheck())
            {
                ScanCounts();
            }

            EditorGUI.BeginChangeCheck();
            _includePrefabs = EditorGUILayout.ToggleLeft("åŒæ—¶å¤„ç† Assets ä¸‹çš„é¢„è®¾ä½“ (Prefab)", _includePrefabs);
            if (EditorGUI.EndChangeCheck())
            {
                ScanCounts();
            }

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _tmpFont != null || _textFont != null;
                if (GUILayout.Button("应用到场景中所有文本", GUILayout.Height(28)))
                {
                    Apply();
                }
                GUI.enabled = true;

                if (GUILayout.Button("重新扫描", GUILayout.Height(28)))
                {
                    ScanCounts();
                }
            }

            EditorGUILayout.Space(8);
            DrawPreviewCounts();
        }
    }

    private void DrawPreviewCounts()
    {
        using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.MinHeight(80)))
        {
            _scroll = scroll.scrollPosition;
            if (!_hasScan)
            {
                EditorGUILayout.HelpBox("未扫描到场景信息，点击“重新扫描”。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"TMP/TMPUI: {_cachedTmpCount}", EditorStyles.label);
            EditorGUILayout.LabelField($"Text: {_cachedTextCount}", EditorStyles.label);
            if (_includePrefabs)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"Prefab TMP/TMPUI: {_cachedPrefabTmpCount}", EditorStyles.label);
                EditorGUILayout.LabelField($"Prefab Text: {_cachedPrefabTextCount}", EditorStyles.label);
            }

            if (_tmpFont == null)
            {
                EditorGUILayout.HelpBox("未选择 TMP 字体：将不会修改 TMP/TMPUI。", MessageType.Info);
            }
            if (_textFont == null)
            {
                EditorGUILayout.HelpBox("未选择 Text 字体：将不会修改 Unity UI Text。", MessageType.Info);
            }
        }
    }

    private void ScanCounts()
    {
        try
        {
            _cachedTmpCount = CollectAllInScenes<TMP_Text>(_onlyActiveScene).Count;
            _cachedTextCount = CollectAllInScenes<Text>(_onlyActiveScene).Count;
            if (_includePrefabs)
            {
                var prefabPaths = FindAllPrefabPathsUnderAssets();
                _cachedPrefabTmpCount = CountAllInPrefabs<TMP_Text>(prefabPaths);
                _cachedPrefabTextCount = CountAllInPrefabs<Text>(prefabPaths);
            }
            else
            {
                _cachedPrefabTmpCount = 0;
                _cachedPrefabTextCount = 0;
            }
            _hasScan = true;
        }
        catch (Exception e)
        {
            _hasScan = false;
            Debug.LogException(e);
        }
        finally
        {
            Repaint();
        }
    }

    private void Apply()
    {
        int changedTmp = 0;
        int changedText = 0;
        int changedPrefabTmp = 0;
        int changedPrefabText = 0;
        var dirtyScenes = new HashSet<Scene>();

        if (_tmpFont != null)
        {
            var tmpTexts = CollectAllInScenes<TMP_Text>(_onlyActiveScene);
            foreach (var t in tmpTexts)
            {
                if (t == null) continue;
                if (t.font == _tmpFont) continue;

                Undo.RecordObject(t, "Batch Change TMP Font");
                t.font = _tmpFont;
                EditorUtility.SetDirty(t);
                changedTmp++;
                if (t.gameObject.scene.IsValid()) dirtyScenes.Add(t.gameObject.scene);
            }
        }

        if (_textFont != null)
        {
            var texts = CollectAllInScenes<Text>(_onlyActiveScene);
            foreach (var t in texts)
            {
                if (t == null) continue;
                if (t.font == _textFont) continue;

                Undo.RecordObject(t, "Batch Change Text Font");
                t.font = _textFont;
                EditorUtility.SetDirty(t);
                changedText++;
                if (t.gameObject.scene.IsValid()) dirtyScenes.Add(t.gameObject.scene);
            }
        }

        if (_includePrefabs && (_tmpFont != null || _textFont != null))
        {
            var prefabPaths = FindAllPrefabPathsUnderAssets();
            try
            {
                for (int i = 0; i < prefabPaths.Count; i++)
                {
                    var path = prefabPaths[i];
                    if (EditorUtility.DisplayCancelableProgressBar("[YusBatchChangeFont] Processing Prefabs", path, prefabPaths.Count <= 1 ? 1f : i / (float)(prefabPaths.Count - 1)))
                        break;

                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        bool changedAny = false;

                        if (_tmpFont != null)
                        {
                            var tmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
                            for (int tIndex = 0; tIndex < tmpTexts.Length; tIndex++)
                            {
                                var t = tmpTexts[tIndex];
                                if (t == null) continue;
                                if (t.font == _tmpFont) continue;

                                t.font = _tmpFont;
                                EditorUtility.SetDirty(t);
                                changedPrefabTmp++;
                                changedAny = true;
                            }
                        }

                        if (_textFont != null)
                        {
                            var texts = root.GetComponentsInChildren<Text>(true);
                            for (int tIndex = 0; tIndex < texts.Length; tIndex++)
                            {
                                var t = texts[tIndex];
                                if (t == null) continue;
                                if (t.font == _textFont) continue;

                                t.font = _textFont;
                                EditorUtility.SetDirty(t);
                                changedPrefabText++;
                                changedAny = true;
                            }
                        }

                        if (changedAny)
                        {
                            PrefabUtility.SaveAsPrefabAsset(root, path);
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
        }

        foreach (var scene in dirtyScenes)
        {
            if (!scene.isLoaded) continue;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        if (_includePrefabs)
        {
            Debug.Log($"[YusBatchChangeFont] Done. Scene TMP changed: {changedTmp}, Scene Text changed: {changedText}, Prefab TMP changed: {changedPrefabTmp}, Prefab Text changed: {changedPrefabText}");
        }
        else
        {
            Debug.Log($"[YusBatchChangeFont] Done. TMP changed: {changedTmp}, Text changed: {changedText}");
        }
        ScanCounts();
    }

    private static List<T> CollectAllInScenes<T>(bool onlyActiveScene) where T : Component
    {
        var result = new List<T>(256);

        if (onlyActiveScene)
        {
            CollectInScene(SceneManager.GetActiveScene(), result);
            return result;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            CollectInScene(scene, result);
        }

        return result;
    }

    private static void CollectInScene<T>(Scene scene, List<T> output) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var root = roots[i];
            if (root == null) continue;
            output.AddRange(root.GetComponentsInChildren<T>(true));
        }
    }

    private static int CountAllInPrefabs<T>(List<string> prefabPaths) where T : Component
    {
        int count = 0;
        for (int i = 0; i < prefabPaths.Count; i++)
        {
            var path = prefabPaths[i];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            count += prefab.GetComponentsInChildren<T>(true).Length;
        }
        return count;
    }

    private static List<string> FindAllPrefabPathsUnderAssets()
    {
        var result = new List<string>(1024);
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path))
                continue;

            var ext = Path.GetExtension(path);
            if (!string.Equals(ext, ".prefab", StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(path);
        }
        return result;
    }
}
#endif
