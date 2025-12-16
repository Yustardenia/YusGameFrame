using UnityEngine;
using TMPro;

namespace YusGameFrame.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedTMPText : MonoBehaviour
    {
        [Tooltip("Excel 中的 Key")]
        public string key;

        [Tooltip("string.Format 参数（可选）")]
        public string[] formatArgs;

        private TMP_Text _textComponent;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            UpdateText();
            this.YusRegister(YusEvents.OnLanguageChanged, UpdateText);
        }

        public void UpdateText()
        {
            if (_textComponent == null || string.IsNullOrEmpty(key)) return;
            if (LocalizationManager.Instance == null) return;

            _textComponent.text = (formatArgs != null && formatArgs.Length > 0)
                ? LocalizationManager.Instance.GetString(key, formatArgs)
                : LocalizationManager.Instance.GetString(key);
        }
    }
}

