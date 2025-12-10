using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BubbleDialogueTable", menuName = "YusData/BubbleDialogueTable")]
public class BubbleDialogueTable : YusTableSO<int, BubbleDialogueData>
{
    public override int GetKey(BubbleDialogueData data) => data.id;
}
