using UnityEngine;
using Fungus;

[CommandInfo("Bubble", "Add Bubble (New)", "基于 Yus架构 的气泡添加")]
public class AddBubbleCommand : Command
{
    [Tooltip("气泡 ID")]
    [SerializeField] protected int bubbleId = 1;

    [Tooltip("是否自动递增 ID")]
    [SerializeField] protected bool autoIncrement = false;

    [Tooltip("文本 (若 ID 在 Excel 有配置，此项可留空)")]
    [SerializeField] protected string text = "";
    [SerializeField] protected string textName = "";
    [SerializeField] protected bool isRight = false;

    // 静态计数器
    private static int _lastAutoId = 0;

    public override void OnEnter()
    {
        int idToUse = bubbleId;

        // 自动递增逻辑：获取 Manager 里的最大 ID + 1
        if (autoIncrement)
        {
            // 如果是第一次运行，同步一下数据库的最大值
            if (_lastAutoId == 0) _lastAutoId = BubbleManager.Instance.GetNextId();
            else _lastAutoId++;
            
            idToUse = _lastAutoId;
        }

        // 一行代码调用业务逻辑
        BubbleManager.Instance.AddBubble(idToUse, text, textName, isRight);

        Continue();
    }
}