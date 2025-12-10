using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BubbleButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI buttonText;  // 按钮显示的文字
    [SerializeField] private Button btn;
    [SerializeField] private RectTransform rectTransform;

    [Header("Data")]
    private int targetId;        // 这条选项对应的气泡 ID
    private string bubbleContent;// 点击后生成的气泡内容
    private string speakerName;  // 说话者名字
    private bool isRight;        // 是否右对齐

    [Header("Settings")]
    [SerializeField] private float fixedWidth = 150f;
    [SerializeField] private float minHeight = 40f;
    [SerializeField] private int maxDisplayLength = 20; // 按钮上文字太长就截断

    private GameObject containerInstance; // 容器引用，用于点击后销毁整个选项面板

    private void Awake()
    {
        if (btn == null) btn = GetComponent<Button>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        
        // 绑定点击事件
        btn.onClick.AddListener(OnButtonClick);
    }

    /// <summary>
    /// 【核心修改】点击按钮的逻辑
    /// </summary>
    private void OnButtonClick()
    {
        // 1. 调用新的 BubbleManager 添加气泡
        // 新的管理器会自动处理：如果 ID 已存在则跳过(虽然选项通常是未存在的)，不存在则保存并通知 UI
        BubbleManager.Instance.AddBubble(targetId, bubbleContent, speakerName, isRight);

        Debug.Log($"[BubbleButton] 点击选项: ID={targetId}, 内容={bubbleContent}");
        // 2. 【核心修改】优雅地回收整个容器和里面的按钮
        if (containerInstance != null)
        {
            RecycleContainerAndChildren(containerInstance);
        }
        else
        {
            // 如果没父容器，只回收自己
            YusPoolManager.Instance.Release(gameObject);
        }
    }
    /// <summary>
    /// 递归回收：先还子物体，再还父物体
    /// </summary>
    private void RecycleContainerAndChildren(GameObject container)
    {
        // 必须倒序遍历或者用 while，因为 Release 会改变 transform.parent
        // 也就是把物体移走，导致 childCount 变化
        while (container.transform.childCount > 0)
        {
            // 获取第一个子物体 (通常是兄弟按钮)
            GameObject child = container.transform.GetChild(0).gameObject;
            
            // 把它还给池子 (PoolManager 会自动把它挪到池子根节点，从而脱离 Container)
            YusPoolManager.Instance.Release(child);
        }

        // 孩子都走光了，现在回收光杆司令 Container
        YusPoolManager.Instance.Release(container);
    }
    
    /// <summary>
    /// 供外部命令调用：设置按钮属性
    /// </summary>
    /// <param name="id">气泡 ID</param>
    /// <param name="btnText">按钮上显示的文字</param>
    /// <param name="bContent">生成气泡的具体内容 (通常和 btnText 一样，或者是它的详细版)</param>
    /// <param name="sName">名字</param>
    /// <param name="right">对齐</param>
    public void SetProperties(int id, string btnText, string bContent, string sName, bool right)
    {
        targetId = id;
        
        // 如果没有单独指定气泡内容，就用按钮文字
        bubbleContent = string.IsNullOrEmpty(bContent) ? btnText : bContent;
        
        speakerName = sName;
        isRight = right;

        // 刷新 UI 显示
        RefreshUI(btnText);
    }

    /// <summary>
    /// 设置父容器引用 (用于点击后销毁)
    /// </summary>
    public void SetContainer(GameObject container)
    {
        containerInstance = container;
    }

    /// <summary>
    /// 刷新按钮的文本和尺寸
    /// </summary>
    private void RefreshUI(string text)
    {
        if (buttonText == null) return;

        // 1. 设置显示文本 (过长截断)
        string display = text;
        if (display.Length > maxDisplayLength)
        {
            display = display.Substring(0, maxDisplayLength) + "...";
        }
        buttonText.text = display;

        // 2. 强制刷新布局以计算高度
        buttonText.ForceMeshUpdate();

        // 3. 简单的自适应高度计算
        // (如果你的 Prefab 用了 ContentSizeFitter，这段其实可以省掉，为了兼容旧逻辑保留)
        float textHeight = buttonText.preferredHeight + 10f; // +padding
        float finalHeight = Mathf.Max(textHeight, minHeight);
        
        rectTransform.sizeDelta = new Vector2(fixedWidth, finalHeight);
    }
}