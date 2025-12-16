namespace YusGameFrame.Localization
{
    // Auto-generated (or kept in sync by generator). Do not edit manually.
    public static class LocalizationDataLanguageAccessor
    {
        public static bool TryGet(global::LocalizationData data, Language language, out string value)
        {
            value = null;
            if (data == null) return false;

            switch (language)
            {
                case Language.zh_cn:
                    value = data.zh_cn;
                    return true;
                case Language.en_us:
                    value = data.en_us;
                    return true;
                default:
                    return false;
            }
        }
    }
}
