using UnityEngine;
using Fungus;
using Fungus.Lua;
using System.Text;
using System.Text.RegularExpressions;

namespace YusGameFrame.Localization
{
    /// <summary>
    /// 将 Yus 本地化系统集成到 Fungus
    /// 使用 {$Key} 语法在 Fungus 对话中引用本地化文本
    /// </summary>
    public class FungusLocalizationBridge : MonoBehaviour, ISubstitutionHandler
    {
        private void OnEnable()
        {
            // 注册到 Fungus 的替换系统 (改为 OnEnable 以确保尽早注册)
            StringSubstituter.RegisterHandler(this);
        }

        private void OnDisable()
        {
            StringSubstituter.UnregisterHandler(this);
        }

        /// <summary>
        /// Fungus 回调此方法来处理字符串替换
        /// </summary>
        public bool SubstituteStrings(StringBuilder input)
        {
            if (LocalizationManager.Instance == null) return false;

            bool modified = false;
            string s = input.ToString();
            
            // 匹配 {$Key} 格式
            // 优化正则：支持点号和连字符，例如 {$Category.Key} 或 {$Key-Name}
            var matches = Regex.Matches(s, @"\{\$([\w\.-]+)\}");
            
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string key = match.Groups[1].Value;
                    
                    // 尝试获取本地化文本
                    string localized = LocalizationManager.Instance.GetString(key);
                    
                    // 只有当内容发生变化时（找到了翻译，或者至少不是 key 本身），才执行替换
                    // 注意：LocalizationManager.GetString 默认找不到返回 Key
                    // 如果我们希望 {$Key} 变成 Key (即去掉花括号)，那么直接替换也是对的
                    
                    // 为了避免死循环或无效替换，我们检查一下
                    if (localized != match.Value) 
                    {
                        input.Replace(match.Value, localized);
                        modified = true;
                    }
                }
            }
            
            return modified;
        }
    }
}
