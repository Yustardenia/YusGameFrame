using UnityEngine;
using Fungus;

namespace YusGameFrame.Localization
{
    /// <summary>
    /// 挂载在 Fungus Character 物体上，自动更新角色名字
    /// </summary>
    [RequireComponent(typeof(Character))]
    public class LocalizedFungusCharacter : MonoBehaviour
    {
        [Tooltip("Excel 中的 Key，例如 CHAR_HERO")]
        public string nameKey;

        private Character _character;

        private void Awake()
        {
            _character = GetComponent<Character>();
        }

        private void Start()
        {
            UpdateName();
            // 使用 YusEventSystem 自动管理事件订阅
            this.YusRegister(YusEvents.OnLanguageChanged, UpdateName);
        }

        private void UpdateName()
        {
            if (_character != null && !string.IsNullOrEmpty(nameKey))
            {
                // Character.NameText 是只读的，但可以通过 SetStandardText 修改
                _character.SetStandardText(LocalizationManager.Instance.GetString(nameKey));
            }
        }
    }
}
