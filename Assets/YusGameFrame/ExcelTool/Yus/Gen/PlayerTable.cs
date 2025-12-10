using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerTable", menuName = "YusData/PlayerTable")]
public class PlayerTable : YusTableSO<int, PlayerData>
{
    public override int GetKey(PlayerData data) => data.id;
}
