using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YusGameFrame.Localization
{
    // Minimal ICU-like formatter:
    // - Named replacement: "{name}"
    // - Plural: "{count, plural, one{...} other{...}}" (supports "=0{...}" exact form)
    // - Escapes: "{{" -> "{", "}}" -> "}"
    public static class IcuMessageFormatter
    {
        public static string Format(string template, IReadOnlyDictionary<string, object> args, Language language)
        {
            if (string.IsNullOrEmpty(template)) return "";
            if (args == null || args.Count == 0) return template;

            return FormatInternal(template, args, language);
        }

        private static string FormatInternal(string template, IReadOnlyDictionary<string, object> args, Language language)
        {
            StringBuilder sb = new StringBuilder(template.Length + 16);

            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];

                if (c == '{')
                {
                    if (i + 1 < template.Length && template[i + 1] == '{')
                    {
                        sb.Append('{');
                        i++;
                        continue;
                    }

                    int end = FindMatchingBrace(template, i);
                    if (end < 0)
                    {
                        sb.Append(c);
                        continue;
                    }

                    string token = template.Substring(i + 1, end - i - 1).Trim();
                    sb.Append(EvaluateToken(token, args, language));
                    i = end;
                    continue;
                }

                if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
                {
                    sb.Append('}');
                    i++;
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static int FindMatchingBrace(string s, int openIndex)
        {
            int depth = 0;
            for (int i = openIndex; i < s.Length; i++)
            {
                char c = s[i];

                if (c == '{')
                {
                    if (i + 1 < s.Length && s[i + 1] == '{')
                    {
                        i++;
                        continue;
                    }

                    depth++;
                    continue;
                }

                if (c == '}')
                {
                    if (i + 1 < s.Length && s[i + 1] == '}')
                    {
                        i++;
                        continue;
                    }

                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }

        private static string EvaluateToken(string token, IReadOnlyDictionary<string, object> args, Language language)
        {
            if (string.IsNullOrEmpty(token)) return "";

            int firstComma = token.IndexOf(',');
            if (firstComma >= 0)
            {
                string varName = token.Substring(0, firstComma).Trim();
                string rest = token.Substring(firstComma + 1).TrimStart();

                if (rest.StartsWith("plural", StringComparison.OrdinalIgnoreCase))
                {
                    int secondComma = rest.IndexOf(',');
                    if (secondComma < 0) return "{" + token + "}";

                    string optionsRaw = rest.Substring(secondComma + 1);
                    return EvaluatePlural(varName, optionsRaw, args, language);
                }
            }

            if (!args.TryGetValue(token, out object value) || value == null)
            {
                return "{" + token + "}";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        private static string EvaluatePlural(
            string varName,
            string optionsRaw,
            IReadOnlyDictionary<string, object> args,
            Language language)
        {
            if (!args.TryGetValue(varName, out object value) || value == null)
            {
                return "{" + varName + "}";
            }

            double n;
            try
            {
                n = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            }

            Dictionary<string, string> options = ParsePluralOptions(optionsRaw);

            string exactKey = "= " + n.ToString(CultureInfo.InvariantCulture);
            exactKey = exactKey.Replace("= ", "=");
            if (options.TryGetValue(exactKey, out string exactBody))
            {
                return FormatInternal(exactBody.Replace("#", n.ToString(CultureInfo.InvariantCulture)), args, language);
            }

            string category = GetPluralCategory(language, n);
            if (!options.TryGetValue(category, out string body) && !options.TryGetValue("other", out body))
            {
                return "";
            }

            body = body.Replace("#", n.ToString(CultureInfo.InvariantCulture));
            return FormatInternal(body, args, language);
        }

        private static Dictionary<string, string> ParsePluralOptions(string optionsRaw)
        {
            Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(optionsRaw)) return options;

            int i = 0;
            while (i < optionsRaw.Length)
            {
                while (i < optionsRaw.Length && char.IsWhiteSpace(optionsRaw[i])) i++;
                if (i >= optionsRaw.Length) break;

                int keyStart = i;
                while (i < optionsRaw.Length && optionsRaw[i] != '{' && !char.IsWhiteSpace(optionsRaw[i])) i++;
                string key = optionsRaw.Substring(keyStart, i - keyStart).Trim();
                if (string.IsNullOrEmpty(key)) break;

                while (i < optionsRaw.Length && char.IsWhiteSpace(optionsRaw[i])) i++;
                if (i >= optionsRaw.Length || optionsRaw[i] != '{') break;

                int open = i;
                int close = FindMatchingBrace(optionsRaw, open);
                if (close < 0) break;

                string body = optionsRaw.Substring(open + 1, close - open - 1);
                options[key] = body;
                i = close + 1;
            }

            return options;
        }

        private static string GetPluralCategory(Language language, double n)
        {
            // Minimal rules for currently supported languages.
            switch (language)
            {
                case Language.en_us:
                    return Math.Abs(n - 1d) < double.Epsilon ? "one" : "other";
                case Language.zh_cn:
                    return "other";
                default:
                    return Math.Abs(n - 1d) < double.Epsilon ? "one" : "other";
            }
        }
    }
}

