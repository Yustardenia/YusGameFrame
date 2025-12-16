using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using YusGameFrame.Localization;

public class BubbleSlider : MonoBehaviour
{
    [Header("资源路径配置 (Resources下)")]
    // 改动 1: 不再直接拖 GameObject，而是填路径
    [Tooltip("例如: UI/LeftBubble")]
    public string leftBubblePath = "UI/LeftBubble"; 
    
    [Tooltip("例如: UI/RightBubble")]
    public string rightBubblePath = "UI/RightBubble";

    public BubblePanel parentPanel; 
    private ScrollRect scrollRect;

    void Awake()
    {
        scrollRect = GetComponentInParent<ScrollRect>();
    }

    // 改动 2: 清空时不能 Destroy，要 Release 回池子
    public void Clear()
    {
        // 注意：在循环中移除子物体，要是用 while 循环最安全
        // 因为 Release 会把物体移出当前父节点(移回池子根节点)
        while (transform.childCount > 0)
        {
            GameObject child = transform.GetChild(0).gameObject;
            YusPoolManager.Instance.Release(child);
        }
    }

    public IEnumerator AddBubbleCoroutine(BubbleDialogueData data)
    {
        // Debug.Log($"[BubbleSlider] 生成气泡: {data.text}");

        // 改动 3: 根据左右选择路径
        string path = data.isRight ? rightBubblePath : leftBubblePath;

        // 改动 4: 从池中获取 (自动处理 Load 和 Instantiate)
        GameObject bubble = YusPoolManager.Instance.Get(path, transform);

        // 获取脚本 (使用 GetInChildren 修正版)
        TextBackground tb = bubble.GetComponentInChildren<TextBackground>();
        
        if (tb != null)
        {
            tb.isRight = data.isRight;
            if (tb.bubbleName != null) tb.bubbleName.text = LocalizationManager.Instance.GetString(data.textName);
            tb.SetText(LocalizationManager.Instance.GetString(data.text));
        }
        else
        {
            YusLogger.Error($"[致命错误] 池对象 {path} 缺少 TextBackground 脚本！");
        }

        // --- 强制刷新布局 (保持不变) ---
        // 即使是对象池复用的物体，因为内容变了，也要重新计算布局
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubble.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        
        if (scrollRect != null)
        {
            yield return new WaitForEndOfFrame();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 历史回放保持不变
    public IEnumerator ReplayHistory(List<BubbleDialogueData> history)
    {
        Clear(); // 这里现在调用的是改过的 Release 逻辑
        foreach (var data in history)
        {
            yield return AddBubbleCoroutine(data);
        }
    }
}
