using UnityEngine;

// 继承 BasePanel，纳入 UI 框架管理
public class BubblePanel : BasePanel
{
    [Header("组件引用")]
    public BubbleSlider bubbleSlider;

    public override void Init()
    {
        base.Init();
        if (bubbleSlider == null) bubbleSlider = GetComponentInChildren<BubbleSlider>();
    }

    public override void Open()
    {
        base.Open();

        BubbleManager.Instance.OnBubbleAdded += HandleNewBubble;
        BubbleManager.Instance.OnHistoryLoaded += RefreshHistory;

        RefreshHistory();
    }

    public override void Close()
    {
        base.Close();

        YusCoroutine.StopOwner(this);

        if (bubbleSlider != null)
        {
            bubbleSlider.Clear();
        }

        if (BubbleManager.Instance != null)
        {
            BubbleManager.Instance.OnBubbleAdded -= HandleNewBubble;
            BubbleManager.Instance.OnHistoryLoaded -= RefreshHistory;
        }
    }

    private void HandleNewBubble(BubbleDialogueData data)
    {
        if (bubbleSlider == null) return;
        YusCoroutine.Run(bubbleSlider.AddBubbleCoroutine(data), this, tag: "BubblePanel.AddBubble");
    }

    private void RefreshHistory()
    {
        if (bubbleSlider == null) return;
        YusCoroutine.Run(bubbleSlider.ReplayHistory(BubbleManager.Instance.DataList), this, tag: "BubblePanel.ReplayHistory");
    }
}
