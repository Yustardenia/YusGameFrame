using UnityEngine;
using UnityEngine.UI;

namespace YusGameFrame.Localization
{
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Excel 中的 Key")]
        public string key;

        [Tooltip("string.Format 参数（可选）")]
        public string[] formatArgs;

        private Text _textComponent;

        private void Awake()
        {
            _textComponent = GetComponent<Text>();
        }

        private void Start()
        {
            UpdateText();
            // 使用 YusEventSystem 自动管理生命周期
            this.YusRegister(YusEvents.OnLanguageChanged, UpdateText);
        }

        // OnDestroy 不需要手动写了，YusRegister 会自动处理

        public void UpdateText()
        {
            if (_textComponent == null || string.IsNullOrEmpty(key)) return;
            
            if (LocalizationManager.Instance != null)
            {
                _textComponent.text = (formatArgs != null && formatArgs.Length > 0)
                    ? LocalizationManager.Instance.GetString(key, formatArgs)
                    : LocalizationManager.Instance.GetString(key);
            }
        }
    }
}
