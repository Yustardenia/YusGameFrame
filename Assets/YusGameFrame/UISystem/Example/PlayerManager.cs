using UnityEngine;
using System;
using Unity.VisualScripting;

// 继承基类，泛型填 PlayerTable 和 PlayerData
public class PlayerManager : YusBaseManager<PlayerTable, PlayerData>
{
    // 单例 (由 YusSingletonManager 统一管理)
    public static PlayerManager Instance { get; private set; }
    private void Awake() 
    { 
        Instance = this; 
    }

    protected override string SaveFileName => "PlayerDataSave";

    // --- 封装一下，方便外部访问 ---
    
    // 因为玩家只有一个，我们取 List 的第一个元素作为“当前玩家”
    public PlayerData CurrentPlayer
    {
        get
        {
            // 如果数据没初始化（比如还没Start），强制初始化一下
            if (DataList == null || DataList.Count == 0) InitData();
            return DataList[0];
        }
    }

    // --- 事件系统：通知 UI 更新 ---
    // UI 监听这个事件，不用每帧去检查数据变没变


    // --- 业务逻辑：修改数据 ---

    public void ChangeHP(int change)
    {
        
        CurrentPlayer.hp  += change;
        if(CurrentPlayer.hp >= CurrentPlayer.maxHp) CurrentPlayer.hp = CurrentPlayer.maxHp;
        if (CurrentPlayer.hp <= 0)
        {
            CurrentPlayer.hp = 0;
            YusEventManager.Instance.Broadcast(YusEvents.OnPlayerDead);
        }
        YusEventManager.Instance.Broadcast(YusEvents.OnPlayerDataChanged);
        Save();
    }

    public void ChangeMP(int change)
    {
        CurrentPlayer.mp  += change;
        YusEventManager.Instance.Broadcast(YusEvents.OnPlayerDataChanged);
        if (CurrentPlayer.mp >= CurrentPlayer.maxMp) CurrentPlayer.mp = CurrentPlayer.maxMp;
        if (CurrentPlayer.mp <= 0)
        {
            CurrentPlayer.mp = 0;
        }
        Save();

    }

    public void ChangeLocation(Vector3 location)
    {
        CurrentPlayer.location = location;
        Save();
    }
    public void ChangeMap(String map)
    {
        CurrentPlayer.map = map;
        Save();
    }

    public void RewriteToExcel()
    {
        Dev_WriteBackToExcel();
    }
}