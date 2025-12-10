using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusPanel : BasePanel
{
    [Header("绑定 UI 组件")]
    public Text nameText;
    public Slider hpSlider;
    public Button healButton;

    [Header("数据源")]
    public PlayerInfoSO playerInfo; // 直接拖入 SO

    public override void Init()
    {
        base.Init();
        
        // 绑定按钮事件
        healButton.onClick.AddListener(() => {
            // UI 反过来修改数据
            playerInfo.UpdateHealth(playerInfo.health + 10);
        });
        
        this.YusRegister(YusEvents.OnPlayerDataChanged,RefreshUI);
    }

    // 当面板打开时，订阅事件并刷新
    public override void Open()
    {
        base.Open();
        RefreshUI(); // 打开时先刷一次
    }

    // 当面板关闭时，取消订阅（防止内存泄漏）
    public override void Close()
    {
        base.Close();
    }

    private void RefreshUI()
    {
        nameText.text = playerInfo.playerName;
        hpSlider.value = (float)playerInfo.health / playerInfo.maxHealth;
    }
}