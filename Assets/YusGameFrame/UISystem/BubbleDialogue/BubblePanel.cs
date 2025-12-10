using UnityEngine;

// 继承 BasePanel，纳入 UI 框架管理
public class BubblePanel : BasePanel
{
    [Header("组件引用")]
    public BubbleSlider bubbleSlider; // 负责具体的 Layout 逻辑

    public override void Init()
    {
        base.Init();
        if (bubbleSlider == null) bubbleSlider = GetComponentInChildren<BubbleSlider>();
    }

    public override void Open()
    {
        base.Open();
        
        // 1. 订阅事件
        BubbleManager.Instance.OnBubbleAdded += HandleNewBubble;
        BubbleManager.Instance.OnHistoryLoaded += RefreshHistory;

        RefreshHistory();
    }

    public override void Close()
    {
        base.Close();
        if (bubbleSlider != null)
        {
            bubbleSlider.Clear(); // 这里的 Clear 已经是调用 Release 了
        }
        // 取消订阅
        if (BubbleManager.Instance != null)
        {
            BubbleManager.Instance.OnBubbleAdded -= HandleNewBubble;
            BubbleManager.Instance.OnHistoryLoaded -= RefreshHistory;
        }
    }

    // 处理单条新增
    private void HandleNewBubble(BubbleDialogueData data)
    {
        StartCoroutine(bubbleSlider.AddBubbleCoroutine(data));
    }

    // 刷新全部历史 (游戏启动时)
    private void RefreshHistory()
    {
        // 告诉 Slider 重新播放所有气泡
        // 注意：BubbleData 和 BubbleText 结构其实是一样的，可以直接传或者转一下
        // 这里假设 BubbleSlider 已经改造成接受 BubbleData
        StartCoroutine(bubbleSlider.ReplayHistory(BubbleManager.Instance.DataList));
    }

    // 更新摄像机边界 (BubbleSlider 跑完协程后调用此方法，或者用 Action 回调)

}