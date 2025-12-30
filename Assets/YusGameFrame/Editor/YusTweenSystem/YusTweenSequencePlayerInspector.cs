#if UNITY_EDITOR && YUS_DOTWEEN
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(YusTweenSequencePlayer))]
public class YusTweenSequencePlayerInspector : Editor
{
    private ReorderableList _tracksList;

    private void OnEnable()
    {
        SerializedProperty tracksProp = serializedObject.FindProperty("tracks");
        _tracksList = new ReorderableList(serializedObject, tracksProp, draggable: true, displayHeader: true, displayAddButton: true, displayRemoveButton: true);

        _tracksList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "轨道（Tracks）");

        _tracksList.elementHeightCallback = index =>
        {
            var element = tracksProp.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, includeChildren: true) + 6f;
        };

        _tracksList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = tracksProp.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height -= 4f;
            EditorGUI.PropertyField(rect, element, new GUIContent($"轨道 {index}"), includeChildren: true);
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Label("触发", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playTrigger"), new GUIContent("播放时机"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnDisable"), new GUIContent("禁用时停止"));

        EditorGUILayout.Space(8);
        GUILayout.Label("序列选项", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("unscaledTime"), new GUIContent("忽略 TimeScale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("linkBehaviour"), new GUIContent("绑定销毁行为"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoKill"), new GUIContent("播放结束后自动销毁"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("killPreviousOnPlay"), new GUIContent("播放前清理旧序列"));

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("自动绑定目标"))
            {
                foreach (Object t in targets)
                {
                    var player = (YusTweenSequencePlayer)t;
                    Undo.RecordObject(player, "自动绑定目标");
                    player.AutoBindTargets();
                    EditorUtility.SetDirty(player);
                }
            }

            if (GUILayout.Button("一键添加：缩放弹出"))
            {
                AddPresetTrack(YusTweenSequencePlayer.TrackType.LocalScale, from: Vector3.zero, to: Vector3.one, easeName: "PopOut");
            }
        }

        EditorGUILayout.Space(6);
        DrawQuickAddRow();

        EditorGUILayout.Space(8);
        _tracksList.DoLayoutList();

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("正向播放"))
                {
                    foreach (Object t in targets) ((YusTweenSequencePlayer)t).PlayForward();
                }

                if (GUILayout.Button("反向播放"))
                {
                    foreach (Object t in targets) ((YusTweenSequencePlayer)t).PlayBackwardFromEnd();
                }

                if (GUILayout.Button("停止"))
                {
                    foreach (Object t in targets) ((YusTweenSequencePlayer)t).Stop();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("播放/预览请在 Play Mode 下使用（不依赖 DOTween Pro 预览）。", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawQuickAddRow()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ 移动")) AddSimpleTrack(YusTweenSequencePlayer.TrackType.LocalMove);
            if (GUILayout.Button("+ 旋转")) AddSimpleTrack(YusTweenSequencePlayer.TrackType.LocalRotate);
            if (GUILayout.Button("+ 缩放")) AddSimpleTrack(YusTweenSequencePlayer.TrackType.LocalScale);
            if (GUILayout.Button("+ 淡入淡出")) AddSimpleTrack(YusTweenSequencePlayer.TrackType.CanvasGroupAlpha);
            if (GUILayout.Button("+ 颜色")) AddSimpleTrack(YusTweenSequencePlayer.TrackType.GraphicColor);
        }
    }

    private void AddSimpleTrack(YusTweenSequencePlayer.TrackType type)
    {
        SerializedProperty tracksProp = serializedObject.FindProperty("tracks");
        int index = tracksProp.arraySize;
        tracksProp.InsertArrayElementAtIndex(index);
        SerializedProperty element = tracksProp.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("type").enumValueIndex = (int)type;
    }

    private void AddPresetTrack(YusTweenSequencePlayer.TrackType type, Vector3 from, Vector3 to, string easeName)
    {
        AddSimpleTrack(type);

        SerializedProperty tracksProp = serializedObject.FindProperty("tracks");
        SerializedProperty element = tracksProp.GetArrayElementAtIndex(tracksProp.arraySize - 1);
        element.FindPropertyRelative("setFrom").boolValue = true;
        element.FindPropertyRelative("fromVector3").vector3Value = from;
        element.FindPropertyRelative("toVector3").vector3Value = to;

        var easeProp = element.FindPropertyRelative("ease");
        if (easeName == "PopOut") easeProp.enumValueIndex = (int)DG.Tweening.Ease.OutBack;
    }
}
#endif
