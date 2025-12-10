using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoPanel : BasePanel
{
    [Header("UI 组件")]
    public Text hpText;
    public Text mpText;
    public Button getDamageButton;
    public Button useMpButton;

    public override void Init()
    {
        base.Init();
        getDamageButton.onClick.AddListener(() =>
        {
            PlayerManager.Instance.ChangeHP(-10);
        });
        useMpButton.onClick.AddListener(() =>
        {
            PlayerManager.Instance.ChangeMP(-10);
        });
        this.YusRegister(YusEvents.OnPlayerDataChanged,UpdateView);
    }

    // 面板打开时：订阅事件 + 刷新一次
    public override void Open()
    {
        base.Open();
        
        // 立即刷新一次显示
        UpdateView();
    }

    // 面板关闭时：取消订阅（防止内存泄漏）
    public override void Close()
    {
        base.Close();
    }

    // 核心：把数据画到 UI 上
    public override void UpdateView()
    {
        // 1. 获取唯一的玩家数据
        var data = PlayerManager.Instance.CurrentPlayer;

        // 2. 显示
        mpText.text = data.mp.ToString();
        hpText.text = $"{data.hp} / {data.maxHp}";
    }
}