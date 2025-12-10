using UnityEngine;
using Fungus;
using System.Collections;
using System.Collections.Generic;

[CommandInfo("Bubble", "Generate Button Container (New)", "基于 Yus架构 的动态按钮生成器。\n1. 检查 ID 是否已存在，存在则跳过。\n2. 生成选项容器。\n3. 等待玩家点击销毁容器后继续。")]
[AddComponentMenu("")]
public class GenerateButtonContainerCommand : Command
{
    [Tooltip("父 GameObject 对象")]
    [SerializeField] protected GameObject parentObject;

    // 改动 1: 换成路径字符串
    [Tooltip("容器预设路径 (e.g. UI/ChoiceContainer)")]
    [SerializeField] protected string buttonContainerPath = "BubbleUI/ButtonContainer";

    [Tooltip("按钮预设路径 (e.g. UI/ChoiceButton)")]
    [SerializeField] protected string buttonPrefabPath = "BubbleUI/ChoiceButton";

    [Tooltip("生成位置偏移")]
    [SerializeField] protected Vector3 containerLocalPosition = Vector3.zero;

    [Header("Data Lists (必须保持长度一致)")]
    [Tooltip("按钮对应气泡 ID (若此 ID 已在存档中，则跳过整个命令)")]
    [SerializeField] protected List<int> buttonIds = new List<int>();

    [Tooltip("按钮上显示的文本")]
    [SerializeField] protected List<string> buttonTexts = new List<string>();

    [Tooltip("点击后生成的气泡内容 (若留空则使用按钮文本)")]
    [SerializeField] protected List<string> buttonCustomTexts = new List<string>();

    [Tooltip("点击后生成的气泡说话者名字")]
    [SerializeField] protected List<string> buttonCustomNames = new List<string>();

    [Tooltip("气泡是否右对齐")]
    [SerializeField] protected List<bool> buttonIsRights = new List<bool>();

    // 运行时引用的容器实例
    private GameObject containerInstance;

    public override void OnEnter()
    {
        // 0. 基础校验
        if (parentObject == null)
        {
            // 尝试自动寻找 Canvas 或 UI Root，防止空引用报错
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null) parentObject = canvas.gameObject;
            else
            {
                YusLogger.Error("[GenerateButton] 未设置父对象且找不到 Canvas，停止执行");
                Continue();
                return;
            }
        }

        int count = buttonIds.Count;
        if (buttonTexts.Count != count || buttonCustomTexts.Count != count)
        {
            YusLogger.Error($"[GenerateButton] 列表长度不一致！IDs:{count}, Texts:{buttonTexts.Count}");
            Continue();
            return;
        }

        // 1. 【核心逻辑】检查历史记录
        // 只要列表里任何一个 ID 已经被记录过（说明玩家之前选过这一组里的某个选项），就跳过
        foreach (int id in buttonIds)
        {
            if (BubbleManager.Instance.HasDialogue(id))
            {
                YusLogger.Log($"[GenerateButton] ID {id} 已存在历史记录中，跳过选项生成。");
                Continue();
                return;
            }
        }

        // 2. 实例化容器
        // 2. 从池中获取容器
        // YusPoolManager.Get(路径, 父节点)
        containerInstance = YusPoolManager.Instance.Get(buttonContainerPath, parentObject.transform);
        
        // 重置位置 (因为复用的对象位置可能乱了)
        containerInstance.transform.localPosition = containerLocalPosition;
        containerInstance.transform.localRotation = Quaternion.identity;
        containerInstance.transform.localScale = Vector3.one;

        // 3. 循环生成按钮
        for (int i = 0; i < buttonIds.Count; i++)
        {
            // 从池中获取按钮，父节点设为刚才的容器
            GameObject btnObj = YusPoolManager.Instance.Get(buttonPrefabPath, containerInstance.transform);
            
            BubbleButton bubbleBtn = btnObj.GetComponent<BubbleButton>();

            if (bubbleBtn != null)
            {
                bubbleBtn.SetProperties(
                    buttonIds[i], 
                    buttonTexts[i], 
                    buttonCustomTexts[i], 
                    buttonCustomNames[i], 
                    buttonIsRights[i]
                );

                // 注入容器引用
                bubbleBtn.SetContainer(containerInstance);
            }
        }

        // 4. 暂停 Fungus 流程，等待玩家选择
        StartCoroutine(WaitForChoice());
    }

    /// <summary>
    /// 等待容器销毁（玩家点击按钮后，BubbleButton 会销毁容器）
    /// </summary>
    private IEnumerator WaitForChoice()
    {
        // 改动 2: 因为对象池回收不会销毁物体，只是禁用
        // 所以我们判断它是否还在层级中显示 (activeInHierarchy)
        // 当 BubbleButton 调用 Release 后，容器会变非激活，循环结束
        while (containerInstance != null && containerInstance.activeInHierarchy)
        {
            yield return null;
        }
        
        Continue();
    }

    public override string GetSummary()
    {
        return $"生成 {buttonIds.Count} 个按钮 (若 ID 存在则跳过)";
    }

    public override Color GetButtonColor()
    {
        return new Color(1f, 0.6f, 0.2f); // 橙色，显眼一点
    }
}