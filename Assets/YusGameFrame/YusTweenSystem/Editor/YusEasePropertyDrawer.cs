#if UNITY_EDITOR && YUS_DOTWEEN
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace YusGameFrame.YusTweenSystem.Editor
{
    /// <summary>
    /// Makes DG.Tweening.Ease show up as Chinese, designer-friendly options in the Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(Ease))]
    public sealed class YusEasePropertyDrawer : PropertyDrawer
    {
        private static GUIContent[] _cachedOptions;
        private static string[] _cachedEnumNames;
        private static Dictionary<string, int> _nameToIndex;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.PropertyField(position, property, label, includeChildren: true);
                return;
            }

            EnsureCache(property);

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                EditorGUI.BeginProperty(position, label, property);

                int currentIndex = Mathf.Clamp(property.enumValueIndex, 0, _cachedOptions.Length - 1);
                bool mixed = property.hasMultipleDifferentValues;

                EditorGUI.showMixedValue = mixed;
                int nextIndex = EditorGUI.Popup(position, label, currentIndex, _cachedOptions);
                EditorGUI.showMixedValue = false;

                if (!mixed && nextIndex != currentIndex)
                {
                    property.enumValueIndex = nextIndex;
                }

                EditorGUI.EndProperty();
            }
        }

        private static void EnsureCache(SerializedProperty property)
        {
            if (_cachedOptions != null && _cachedEnumNames != null && _nameToIndex != null) return;

            _cachedEnumNames = property.enumNames;
            _cachedOptions = new GUIContent[_cachedEnumNames.Length];
            _nameToIndex = new Dictionary<string, int>(_cachedEnumNames.Length, StringComparer.Ordinal);

            for (int i = 0; i < _cachedEnumNames.Length; i++)
            {
                string enumName = _cachedEnumNames[i];
                _nameToIndex[enumName] = i;

                if (TryParseEase(enumName, out Ease ease))
                {
                    var (cnTitle, cnTooltip) = GetChineseText(ease);
                    if (!string.IsNullOrWhiteSpace(cnTitle))
                    {
                        _cachedOptions[i] = new GUIContent($"{cnTitle}（{enumName}）", cnTooltip);
                        continue;
                    }
                }

                _cachedOptions[i] = new GUIContent(enumName);
            }
        }

        private static bool TryParseEase(string enumName, out Ease ease)
        {
            try
            {
                ease = (Ease)Enum.Parse(typeof(Ease), enumName, ignoreCase: false);
                return true;
            }
            catch
            {
                ease = default;
                return false;
            }
        }

        private static (string title, string tooltip) GetChineseText(Ease ease)
        {
            // Project-curated "human" names first (matches Assets/YusGameFrame/YusTweenSystem/YusEase.cs).
            switch (ease)
            {
                case Ease.Linear:
                    return ("匀速", "不拐弯、不加速：一直保持同样速度。");

                case Ease.InQuad:
                    return ("慢到快", "先慢后快：起步稳、后段加速。");
                case Ease.OutQuad:
                    return ("快到慢", "先快后慢：先冲出去、后段刹车。");

                case Ease.OutCubic:
                    return ("利落停下", "先快后慢、收尾更干脆：适合大部分 UI/移动默认。");
                case Ease.InCubic:
                    return ("快速收回", "收尾像被吸回去：适合关闭/消失。");

                case Ease.OutBack:
                    return ("弹一下（向外）", "带一点夸张回弹：弹窗出现/按钮强调。");
                case Ease.InBack:
                    return ("弹一下（向内）", "像把弹簧往里按：缩回/消失的收尾。");

                case Ease.InExpo:
                    return ("火箭起步", "前期非常慢、后期非常快：突然冲刺的感觉。");
                case Ease.OutExpo:
                    return ("火箭刹车", "前期很快、后期很慢：冲出去然后稳稳停住。");
            }

            // Generic pattern-based translations for the rest of DOTween's Ease enum.
            string name = ease.ToString();

            if (name == "Unset") return ("未设置", "使用默认缓动。");
            if (name == "Internal_Zero") return ("内部：0", "DOTween 内部占位/特殊值。");

            string direction;
            string family;

            if (!TrySplitEaseName(name, out direction, out family))
            {
                return (null, null);
            }

            string directionCn = direction switch
            {
                "In" => "慢到快",
                "Out" => "快到慢",
                "InOut" => "慢→快→慢",
                "OutIn" => "快→慢→快",
                _ => null
            };

            string familyCn = family switch
            {
                "Sine" => "正弦（柔和）",
                "Quad" => "二次（轻）",
                "Cubic" => "三次（更利落）",
                "Quart" => "四次（更强）",
                "Quint" => "五次（很强）",
                "Expo" => "指数（很猛）",
                "Circ" => "圆弧（自然）",
                "Back" => "回弹（夸张）",
                "Elastic" => "弹簧（抖动）",
                "Bounce" => "弹跳（落地）",
                "Flash" => "闪动（忽快忽慢）",
                "Spring" => "弹簧（自然回弹）",
                _ => null
            };

            if (directionCn == null && familyCn == null) return (null, null);
            if (directionCn == null) return (familyCn, null);
            if (familyCn == null) return (directionCn, null);

            return ($"{directionCn} · {familyCn}", null);
        }

        private static bool TrySplitEaseName(string easeName, out string direction, out string family)
        {
            // Most DOTween ease names are like "InQuad", "OutCubic", "InOutSine", "OutInBounce".
            if (easeName.StartsWith("InOut", StringComparison.Ordinal))
            {
                direction = "InOut";
                family = easeName.Substring("InOut".Length);
                return family.Length > 0;
            }

            if (easeName.StartsWith("OutIn", StringComparison.Ordinal))
            {
                direction = "OutIn";
                family = easeName.Substring("OutIn".Length);
                return family.Length > 0;
            }

            if (easeName.StartsWith("In", StringComparison.Ordinal))
            {
                direction = "In";
                family = easeName.Substring("In".Length);
                return family.Length > 0;
            }

            if (easeName.StartsWith("Out", StringComparison.Ordinal))
            {
                direction = "Out";
                family = easeName.Substring("Out".Length);
                return family.Length > 0;
            }

            direction = null;
            family = null;
            return false;
        }
    }
}
#endif

