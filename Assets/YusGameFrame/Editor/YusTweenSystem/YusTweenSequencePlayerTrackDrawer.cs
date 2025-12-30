#if UNITY_EDITOR && YUS_DOTWEEN
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(YusTweenSequencePlayer.Track))]
public class YusTweenSequencePlayerTrackDrawer : PropertyDrawer
{
    private static float Line => EditorGUIUtility.singleLineHeight;
    private static float Gap => EditorGUIUtility.standardVerticalSpacing;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = 0f;

        height += Line + Gap; // header row
        height += (Line + Gap) * 3; // timing (delay/duration/ease)
        height += (Line + Gap) * 2; // loops (loops/loopType)
        height += (Line + Gap) * 2; // value options (setFrom/relative)

        var typeProp = property.FindPropertyRelative("type");
        var type = (YusTweenSequencePlayer.TrackType)typeProp.enumValueIndex;
        bool showFrom = property.FindPropertyRelative("setFrom").boolValue;

        switch (type)
        {
            case YusTweenSequencePlayer.TrackType.LocalMove:
            case YusTweenSequencePlayer.TrackType.LocalRotate:
            case YusTweenSequencePlayer.TrackType.LocalScale:
                height += (Line + Gap) * (showFrom ? 2 : 1); // from/to (Vector3)
                height += Line + Gap; // target
                break;
            case YusTweenSequencePlayer.TrackType.AnchoredPosition:
                height += (Line + Gap) * (showFrom ? 2 : 1); // from/to (Vector2)
                height += Line + Gap; // target
                break;
            case YusTweenSequencePlayer.TrackType.CanvasGroupAlpha:
                height += (Line + Gap) * (showFrom ? 2 : 1); // from/to alpha
                height += Line + Gap; // target
                break;
            case YusTweenSequencePlayer.TrackType.GraphicColor:
            case YusTweenSequencePlayer.TrackType.SpriteRendererColor:
                height += (Line + Gap) * (showFrom ? 2 : 1); // from/to color
                height += Line + Gap; // target
                break;
            case YusTweenSequencePlayer.TrackType.RendererColor:
                height += (Line + Gap) * (showFrom ? 2 : 1); // from/to color
                height += (Line + Gap) * 2; // target + shared
                break;
        }

        height += Line + Gap; // safety

        return height + 6f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using (new EditorGUI.PropertyScope(position, label, property))
        {
            int oldIndent = EditorGUI.indentLevel;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = Mathf.Max(88f, Mathf.Min(130f, position.width * 0.35f));

            var enabledProp = property.FindPropertyRelative("enabled");
            var typeProp = property.FindPropertyRelative("type");

            var delayProp = property.FindPropertyRelative("delay");
            var durationProp = property.FindPropertyRelative("duration");
            var easeProp = property.FindPropertyRelative("ease");

            var loopsProp = property.FindPropertyRelative("loops");
            var loopTypeProp = property.FindPropertyRelative("loopType");

            var setFromProp = property.FindPropertyRelative("setFrom");
            var relativeProp = property.FindPropertyRelative("relative");

            var fromV3Prop = property.FindPropertyRelative("fromVector3");
            var toV3Prop = property.FindPropertyRelative("toVector3");
            var fromV2Prop = property.FindPropertyRelative("fromVector2");
            var toV2Prop = property.FindPropertyRelative("toVector2");
            var fromAlphaProp = property.FindPropertyRelative("fromAlpha");
            var toAlphaProp = property.FindPropertyRelative("toAlpha");
            var fromColorProp = property.FindPropertyRelative("fromColor");
            var toColorProp = property.FindPropertyRelative("toColor");

            var transformTargetProp = property.FindPropertyRelative("transformTarget");
            var rectTargetProp = property.FindPropertyRelative("rectTransformTarget");
            var canvasGroupTargetProp = property.FindPropertyRelative("canvasGroupTarget");
            var graphicTargetProp = property.FindPropertyRelative("graphicTarget");
            var spriteRendererTargetProp = property.FindPropertyRelative("spriteRendererTarget");
            var rendererTargetProp = property.FindPropertyRelative("rendererTarget");
            var rendererUseSharedProp = property.FindPropertyRelative("rendererUseSharedMaterial");

            var killTargetTweensProp = property.FindPropertyRelative("killTargetTweens");

            Rect r = position;
            r.height = Line;

            // Header: enabled + type
            Rect enabledRect = new Rect(r.x, r.y, 18f, r.height);
            enabledProp.boolValue = EditorGUI.Toggle(enabledRect, enabledProp.boolValue);

            Rect typeRect = new Rect(r.x + 22f, r.y, r.width - 22f, r.height);
            EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("类型"));
            r.y += Line + Gap;

            // Timing
            EditorGUI.PropertyField(r, delayProp, new GUIContent("延迟"));
            r.y += Line;
            EditorGUI.PropertyField(r, durationProp, new GUIContent("时长"));
            r.y += Line;
            EditorGUI.PropertyField(r, easeProp, new GUIContent("缓动"));
            r.y += Line + Gap;

            // Loops
            EditorGUI.PropertyField(r, loopsProp, new GUIContent("循环次数", "0=不循环，-1=无限"));
            r.y += Line;
            EditorGUI.PropertyField(r, loopTypeProp, new GUIContent("循环方式"));
            r.y += Line + Gap;

            // Value options
            EditorGUI.PropertyField(r, setFromProp, new GUIContent("指定起点", "勾选后会先把目标设置为 From 再开始 Tween"));
            r.y += Line;
            EditorGUI.PropertyField(r, relativeProp, new GUIContent("相对值", "等同于 DOTween 的 SetRelative()"));
            r.y += Line + Gap;

            var type = (YusTweenSequencePlayer.TrackType)typeProp.enumValueIndex;
            switch (type)
            {
                case YusTweenSequencePlayer.TrackType.LocalMove:
                    DrawFromToV3(ref r, setFromProp.boolValue, fromV3Prop, toV3Prop, "From", "To（本地坐标）");
                    EditorGUI.PropertyField(r, transformTargetProp, new GUIContent("目标 Transform"));
                    r.y += Line + Gap;
                    break;
                case YusTweenSequencePlayer.TrackType.LocalRotate:
                    DrawFromToV3(ref r, setFromProp.boolValue, fromV3Prop, toV3Prop, "From", "To（本地欧拉角）");
                    EditorGUI.PropertyField(r, transformTargetProp, new GUIContent("目标 Transform"));
                    r.y += Line + Gap;
                    break;
                case YusTweenSequencePlayer.TrackType.LocalScale:
                    DrawFromToV3(ref r, setFromProp.boolValue, fromV3Prop, toV3Prop, "From", "To（缩放）");
                    EditorGUI.PropertyField(r, transformTargetProp, new GUIContent("目标 Transform"));
                    r.y += Line + Gap;
                    break;
                case YusTweenSequencePlayer.TrackType.AnchoredPosition:
                    DrawFromToV2(ref r, setFromProp.boolValue, fromV2Prop, toV2Prop, "From", "To（AnchoredPos）");
                    EditorGUI.PropertyField(r, rectTargetProp, new GUIContent("目标 RectTransform"));
                    r.y += Line + Gap;
                    break;
                case YusTweenSequencePlayer.TrackType.CanvasGroupAlpha:
                    DrawFromToAlpha(ref r, setFromProp.boolValue, fromAlphaProp, toAlphaProp);
                    EditorGUI.PropertyField(r, canvasGroupTargetProp, new GUIContent("目标 CanvasGroup"));
                    r.y += Line + Gap;
                    break;
                case YusTweenSequencePlayer.TrackType.GraphicColor:
                    DrawFromToColor(ref r, setFromProp.boolValue, fromColorProp, toColorProp);
                    EditorGUI.PropertyField(r, graphicTargetProp, new GUIContent("目标 Graphic"));
                    r.y += Line + Gap;
                    break;
                case YusTweenSequencePlayer.TrackType.SpriteRendererColor:
                    DrawFromToColor(ref r, setFromProp.boolValue, fromColorProp, toColorProp);
                    EditorGUI.PropertyField(r, spriteRendererTargetProp, new GUIContent("目标 SpriteRenderer"));
                    r.y += Line + Gap;
                    break;
                case YusTweenSequencePlayer.TrackType.RendererColor:
                    DrawFromToColor(ref r, setFromProp.boolValue, fromColorProp, toColorProp);
                    EditorGUI.PropertyField(r, rendererTargetProp, new GUIContent("目标 Renderer"));
                    r.y += Line;
                    EditorGUI.PropertyField(r, rendererUseSharedProp, new GUIContent("使用 SharedMaterial", "谨慎：会影响共享材质，可能改到一大片对象"));
                    r.y += Line + Gap;
                    break;
            }

            EditorGUI.PropertyField(r, killTargetTweensProp, new GUIContent("播放前清理目标 Tween"));

            EditorGUI.indentLevel = oldIndent;
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }
    }

    private static void DrawFromToV3(ref Rect r, bool showFrom, SerializedProperty from, SerializedProperty to, string fromLabel, string toLabel)
    {
        if (showFrom)
        {
            EditorGUI.PropertyField(r, from, new GUIContent(fromLabel));
            r.y += Line;
        }

        EditorGUI.PropertyField(r, to, new GUIContent(toLabel));
        r.y += Line + Gap;
    }

    private static void DrawFromToV2(ref Rect r, bool showFrom, SerializedProperty from, SerializedProperty to, string fromLabel, string toLabel)
    {
        if (showFrom)
        {
            EditorGUI.PropertyField(r, from, new GUIContent(fromLabel));
            r.y += Line;
        }

        EditorGUI.PropertyField(r, to, new GUIContent(toLabel));
        r.y += Line + Gap;
    }

    private static void DrawFromToAlpha(ref Rect r, bool showFrom, SerializedProperty from, SerializedProperty to)
    {
        if (showFrom)
        {
            EditorGUI.Slider(r, from, 0f, 1f, new GUIContent("From（Alpha）"));
            r.y += Line;
        }

        EditorGUI.Slider(r, to, 0f, 1f, new GUIContent("To（Alpha）"));
        r.y += Line + Gap;
    }

    private static void DrawFromToColor(ref Rect r, bool showFrom, SerializedProperty from, SerializedProperty to)
    {
        if (showFrom)
        {
            EditorGUI.PropertyField(r, from, new GUIContent("颜色 From"));
            r.y += Line;
        }

        EditorGUI.PropertyField(r, to, new GUIContent("颜色 To"));
        r.y += Line + Gap;
    }
}
#endif
