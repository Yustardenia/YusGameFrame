using UnityEngine;
using System;

[CreateAssetMenu(menuName = "GameData/PlayerInfo")]
public class PlayerInfoSO : ScriptableObject
{
    public string playerName;
    public int health;
    public int maxHealth;
    

    public void UpdateHealth(int newHealth)
    {
        health = Mathf.Clamp(newHealth, 0, maxHealth);
        YusEventManager.Instance.Broadcast(YusEvents.OnPlayerDataChanged);
    }
}