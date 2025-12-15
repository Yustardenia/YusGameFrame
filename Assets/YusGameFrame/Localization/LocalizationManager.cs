using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace YusGameFrame.Localization
{
    public class LocalizationManager : YusBaseManager<LocalizationTable, LocalizationData>
    {
        public static LocalizationManager Instance { get; private set; }

        [Header("当前语言")]
        public Language currentLanguage = Language.zh_cn;

        // 优化：直接缓存 Key -> 当前语言文本，避免每次 GetString 都反射
        private Dictionary<string, string> _localizedStrings = new Dictionary<string, string>();

        protected override string SaveFileName => "LocalizationSave"; 
        private const string PREFS_KEY = "Yus_Language_Setting";

        private void Awake()
        {
            Instance = this;
            LoadLanguageSetting();
        }

        private void LoadLanguageSetting()
        {
            try
            {
                if (PlayerPrefs.HasKey(PREFS_KEY))
                {
                    string langStr = PlayerPrefs.GetString(PREFS_KEY);
                    if (System.Enum.TryParse(langStr, out Language savedLang))
                    {
                        currentLanguage = savedLang;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocalizationManager] 加载语言设置失败: {e.Message}");
            }
        }

        protected override void OnLoadSuccess(bool isNewGame)
        {
            RefreshLocalizedStrings();
            
            Debug.Log($"[LocalizationManager] 加载了 {_localizedStrings.Count} 条本地化数据。当前语言: {currentLanguage}");
            
            // 广播一次事件，确保那些比管理器先初始化的 UI 组件能得到刷新
            if (YusEventManager.Instance != null)
            {
                YusEventManager.Instance.Broadcast(YusEvents.OnLanguageChanged);
            }
        }

        private void RefreshLocalizedStrings()
        {
            _localizedStrings.Clear();

            if (DataList == null || DataList.Count == 0) return;

            // 1. 获取当前语言对应的字段信息 (只反射一次)
            System.Type dataType = typeof(LocalizationData);
            FieldInfo langField = dataType.GetField(currentLanguage.ToString());

            if (langField == null)
            {
                Debug.LogError($"[LocalizationManager] 找不到语言字段: {currentLanguage}。请检查 LocalizationData 类是否包含该字段。");
                return;
            }

            // 2. 遍历数据并缓存
            foreach (var data in DataList)
            {
                if (data == null) continue;

                // 直接访问 key 字段
                string keyVal = data.key; 

                if (string.IsNullOrEmpty(keyVal)) continue;

                // 获取对应语言的值
                string localizedVal = langField.GetValue(data) as string;

                // 如果翻译存在，则存入
                if (!string.IsNullOrEmpty(localizedVal))
                {
                    _localizedStrings[keyVal] = localizedVal;
                }
            }
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        /// <param name="key">Excel中的Key</param>
        /// <returns>对应语言的文本，如果找不到则返回Key本身</returns>
        public string GetString(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            if (_localizedStrings.TryGetValue(key, out string val))
            {
                return val;
            }
            
            // 找不到时的警告 (可选，开发模式下开启)
            #if UNITY_EDITOR
            // Debug.LogWarning($"[LocalizationManager] 缺少翻译 Key: {key} (Language: {currentLanguage})");
            #endif

            // 找不到则返回 key 自身
            return key;
        }

        public void ChangeLanguage(Language newLang)
        {
            if (currentLanguage == newLang) return;
            currentLanguage = newLang;
            
            // 刷新缓存
            RefreshLocalizedStrings();

            // 保存设置
            try
            {
                PlayerPrefs.SetString(PREFS_KEY, currentLanguage.ToString());
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocalizationManager] 保存语言设置失败: {e.Message}");
            }

            YusEventManager.Instance.Broadcast(YusEvents.OnLanguageChanged);
        }
    }
}
