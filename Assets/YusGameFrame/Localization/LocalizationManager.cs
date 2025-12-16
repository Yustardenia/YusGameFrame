using System;
using System.Collections.Generic;
using UnityEngine;

namespace YusGameFrame.Localization
{
    public class LocalizationManager : YusBaseManager<LocalizationTable, LocalizationData>
    {
        public static LocalizationManager Instance { get; private set; }

        [Header("当前语言")]
        public Language currentLanguage = Language.zh_cn;

        [Header("默认语言 / Fallback")]
        public Language defaultLanguage = Language.zh_cn;

        [SerializeField] private List<LanguageFallbackRule> fallbackRules = new List<LanguageFallbackRule>();

        protected override string SaveFileName => "LocalizationSave";
        private const string PREFS_KEY = "Yus_Language_Setting";

        private readonly Dictionary<string, LocalizationData> _dataByKey =
            new Dictionary<string, LocalizationData>(StringComparer.Ordinal);

        private void Awake()
        {
            Instance = this;
            LoadLanguageSetting();
        }

        private void LoadLanguageSetting()
        {
            try
            {
                if (!PlayerPrefs.HasKey(PREFS_KEY)) return;

                string langStr = PlayerPrefs.GetString(PREFS_KEY);
                if (Enum.TryParse(langStr, out Language savedLang))
                {
                    currentLanguage = savedLang;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalizationManager] 加载语言设置失败: {e.Message}");
            }
        }

        public override void InitData()
        {
            DataList = YusDataManager.Instance.CreateRuntimeListFromConfig<LocalizationTable, LocalizationData>();
            OnLoadSuccess(true);
        }

        protected override void OnLoadSuccess(bool isNewGame)
        {
            BuildKeyIndex();

            Debug.Log($"[LocalizationManager] 加载了 {_dataByKey.Count} 条本地化数据。当前语言: {currentLanguage}");

            if (YusEventManager.Instance != null)
            {
                YusEventManager.Instance.Broadcast(YusEvents.OnLanguageChanged);
            }
        }

        private void BuildKeyIndex()
        {
            _dataByKey.Clear();
            if (DataList == null || DataList.Count == 0) return;

            foreach (var data in DataList)
            {
                if (data == null) continue;

                string keyVal = data.key;
                if (string.IsNullOrEmpty(keyVal)) continue;

                if (_dataByKey.ContainsKey(keyVal))
                {
                    Debug.LogWarning($"[LocalizationManager] 重复 Key: {keyVal}，将以后者覆盖。");
                }

                _dataByKey[keyVal] = data;
            }
        }

        private IEnumerable<Language> EnumerateFallbackChain(Language fromLanguage)
        {
            HashSet<Language> visited = new HashSet<Language>();
            visited.Add(fromLanguage);

            for (int i = 0; i < fallbackRules.Count; i++)
            {
                var rule = fallbackRules[i];
                if (rule == null) continue;
                if (rule.from != fromLanguage) continue;

                if (rule.to != null)
                {
                    for (int j = 0; j < rule.to.Count; j++)
                    {
                        var lang = rule.to[j];
                        if (visited.Add(lang))
                        {
                            yield return lang;
                        }
                    }
                }

                break;
            }

            if (visited.Add(defaultLanguage))
            {
                yield return defaultLanguage;
            }
        }

        private string GetRawString(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            if (!_dataByKey.TryGetValue(key, out LocalizationData data) || data == null)
            {
                return key;
            }

            if (LocalizationDataLanguageAccessor.TryGet(data, currentLanguage, out string val) &&
                !string.IsNullOrEmpty(val))
            {
                return val;
            }

            foreach (var fallbackLang in EnumerateFallbackChain(currentLanguage))
            {
                if (LocalizationDataLanguageAccessor.TryGet(data, fallbackLang, out string fallbackVal) &&
                    !string.IsNullOrEmpty(fallbackVal))
                {
                    return fallbackVal;
                }
            }

            return key;
        }

        public string GetString(string key)
        {
            return GetRawString(key);
        }

        public string GetString(string key, params object[] args)
        {
            string raw = GetRawString(key);
            if (args == null || args.Length == 0) return raw;

            try
            {
                return string.Format(raw, args);
            }
            catch (Exception)
            {
                return raw;
            }
        }

        public string GetStringIcu(string key, IReadOnlyDictionary<string, object> args)
        {
            string raw = GetRawString(key);
            return IcuMessageFormatter.Format(raw, args, currentLanguage);
        }

        public void ChangeLanguage(Language newLang)
        {
            if (currentLanguage == newLang) return;
            currentLanguage = newLang;

            try
            {
                PlayerPrefs.SetString(PREFS_KEY, currentLanguage.ToString());
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalizationManager] 保存语言设置失败: {e.Message}");
            }

            YusEventManager.Instance.Broadcast(YusEvents.OnLanguageChanged);
        }
    }

    [Serializable]
    public class LanguageFallbackRule
    {
        public Language from;
        public List<Language> to = new List<Language>();
    }
}
