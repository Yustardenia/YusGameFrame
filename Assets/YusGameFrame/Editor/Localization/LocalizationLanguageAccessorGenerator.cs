#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace YusGameFrame.Localization.Editor
{
    public static class LocalizationLanguageAccessorGenerator
    {
        private const string OutputPath = "Assets/YusGameFrame/Localization/Gen/LocalizationDataLanguageAccessor.g.cs";

        [InitializeOnLoadMethod]
        private static void AutoGenerateOnLoad()
        {
            GenerateIfNeeded(false);
        }

    [MenuItem(YusGameFrameEditorMenu.Root + "Data/Localization/重载本地化语言访问器")]
        public static void Regenerate()
        {
            GenerateIfNeeded(true);
        }

        private static void GenerateIfNeeded(bool force)
        {
            try
            {
                string code = GenerateCode(out List<string> warnings);

                if (!force && File.Exists(OutputPath))
                {
                    string existing = File.ReadAllText(OutputPath, Encoding.UTF8);
                    if (existing == code) return;
                }

                string dir = Path.GetDirectoryName(OutputPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(OutputPath, code, new UTF8Encoding(true));
                AssetDatabase.ImportAsset(OutputPath);

                for (int i = 0; i < warnings.Count; i++)
                {
                    Debug.LogWarning(warnings[i]);
                }

                Debug.Log($"[Localization] Generated: {OutputPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Localization] Generate failed: {e}");
            }
        }

        private static string GenerateCode(out List<string> warnings)
        {
            warnings = new List<string>();

            var dataType = typeof(global::LocalizationData);
            HashSet<string> dataStringFields = new HashSet<string>(
                dataType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => f.FieldType == typeof(string))
                    .Select(f => f.Name),
                StringComparer.Ordinal);

            Array langs = Enum.GetValues(typeof(Language));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("namespace YusGameFrame.Localization");
            sb.AppendLine("{");
            sb.AppendLine("    // Auto-generated (or kept in sync by generator). Do not edit manually.");
            sb.AppendLine("    public static class LocalizationDataLanguageAccessor");
            sb.AppendLine("    {");
            sb.AppendLine("        public static bool TryGet(global::LocalizationData data, Language language, out string value)");
            sb.AppendLine("        {");
            sb.AppendLine("            value = null;");
            sb.AppendLine("            if (data == null) return false;");
            sb.AppendLine();
            sb.AppendLine("            switch (language)");
            sb.AppendLine("            {");

            for (int i = 0; i < langs.Length; i++)
            {
                var lang = (Language)langs.GetValue(i);
                string name = lang.ToString();

                if (!dataStringFields.Contains(name))
                {
                    warnings.Add($"[Localization] Language '{name}' has no field on LocalizationData.");
                    continue;
                }

                sb.AppendLine($"                case Language.{name}:");
                sb.AppendLine($"                    value = data.{name};");
                sb.AppendLine("                    return true;");
            }

            sb.AppendLine("                default:");
            sb.AppendLine("                    return false;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
#endif
