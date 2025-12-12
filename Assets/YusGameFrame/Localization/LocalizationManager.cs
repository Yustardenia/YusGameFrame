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

        // 缓存：Key -> Data
        private Dictionary<string, LocalizationData> _keyMap = new Dictionary<string, LocalizationData>();

        protected override string SaveFileName => "LocalizationSave"; 
        private const string PREFS_KEY = "Yus_Language_Setting";

        private void Awake()
        {
            Instance = this;
            // 读取语言设置
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string langStr = PlayerPrefs.GetString(PREFS_KEY);
                if (System.Enum.TryParse(langStr, out Language savedLang))
                {
                    currentLanguage = savedLang;
                }
            }
        }

        protected override void OnLoadSuccess(bool isNewGame)
        {
            _keyMap.Clear();
            // 假设 LocalizationData 有一个名为 'key' 的字段
            // 我们通过反射或者假设用户会在 Excel 里把 Key 列命名为 'key'
            foreach (var data in DataList)
            {
                // 为了通用性，我们这里用反射获取 key，或者直接访问 data.key (如果确定有)
                // 这里我们假设生成的类有 key 字段
                var keyField = typeof(LocalizationData).GetField("key");
                if (keyField != null)
                {
                    string keyVal = keyField.GetValue(data) as string;
                    if (!string.IsNullOrEmpty(keyVal) && !_keyMap.ContainsKey(keyVal))
                    {
                        _keyMap.Add(keyVal, data);
                    }
                }
            }
            
            Debug.Log($"[LocalizationManager] 加载了 {_keyMap.Count} 条本地化数据。当前语言: {currentLanguage}");
            
            // 广播一次事件，确保那些比管理器先初始化的 UI 组件能得到刷新
            if (YusEventManager.Instance != null)
            {
                YusEventManager.Instance.Broadcast(YusEvents.OnLanguageChanged);
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

            if (_keyMap.TryGetValue(key, out var data))
            {
                // 获取当前语言对应的字段
                var langField = typeof(LocalizationData).GetField(currentLanguage.ToString());
                if (langField != null)
                {
                    string val = langField.GetValue(data) as string;
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
            
            // 找不到则返回 key 自身
            return key;
        }

        public void ChangeLanguage(Language newLang)
        {
            if (currentLanguage == newLang) return;
            currentLanguage = newLang;
            
            // 保存设置
            PlayerPrefs.SetString(PREFS_KEY, currentLanguage.ToString());
            PlayerPrefs.Save();

            YusEventManager.Instance.Broadcast(YusEvents.OnLanguageChanged);
        }
    }
}
