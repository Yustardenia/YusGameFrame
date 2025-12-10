using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways] // 让它在编辑模式下也能实时刷新，方便调试
public class TextBackground : MonoBehaviour
{
    [Header("组件引用")]
    public TextMeshProUGUI textComponent;
    public TextMeshProUGUI bubbleName; // 可选
    public Image backgroundImage;      // 可选，如果背景就是挂脚本的物体，这个其实不用填
    public bool isRight;               // 用于区分左右气泡（改颜色或对齐）

    [Header("设置")]
    public float maxWidth = 400f; // 最大宽度限制

    private LayoutElement _layoutElement;
    private RectTransform _rectTransform;

    void OnEnable()
    {
        if (textComponent == null) return;
        
        // 自动获取或添加 LayoutElement
        _layoutElement = textComponent.GetComponent<LayoutElement>();
        if (_layoutElement == null) _layoutElement = textComponent.gameObject.AddComponent<LayoutElement>();
        
        _rectTransform = textComponent.GetComponent<RectTransform>();
    }

    void Update()
    {
        if (textComponent == null || _layoutElement == null) return;

        // --- 核心修复逻辑 ---
        
        // 1. 询问 TMP：如果不换行，这段文字到底有多宽？
        // GetPreferredValues(text, widthLimit, heightLimit)
        Vector2 preferredSize = textComponent.GetPreferredValues(textComponent.text, Mathf.Infinity, Mathf.Infinity);
        
        // 2. 判断逻辑
        if (preferredSize.x > maxWidth)
        {
            // 如果文字本来就比最大宽度宽 -> 限制 LayoutElement 宽度为 Max
            // 这样父级 LayoutGroup 就会把容器限制在 400，TMP 就会自动换行
            _layoutElement.preferredWidth = maxWidth;
            _layoutElement.enabled = true; // 启用限制
        }
        else
        {
            // 如果文字很短 -> 取消 LayoutElement 的宽度限制
            // 让父级 ContentSizeFitter 使用 TMP 自身的 preferredWidth (自适应变窄)
            _layoutElement.preferredWidth = -1; // -1 代表不覆盖，使用原始大小
            _layoutElement.enabled = false; // 或者直接禁用 LayoutElement 对宽度的控制
        }
        
        // 名字设置（可选）
        if (bubbleName != null)
        {
            // 可以在这里处理名字的显隐或颜色
        }
    }

    public void SetText(string newText)
    {
        // 再次打印确认
        YusLogger.Log($"[TextBackground] SetText 被调用，新文本: {newText}");

        if (textComponent != null)
        {
            textComponent.text = newText;
            
            // 强制刷新，确保文字变了之后布局能跟上
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);
            
            // 如果你有 layoutElement 逻辑，确保 update 里能检测到变化
        }
        else
        {
            YusLogger.Error("[TextBackground] textComponent 是空的！无法赋值！");
        }
    }
}