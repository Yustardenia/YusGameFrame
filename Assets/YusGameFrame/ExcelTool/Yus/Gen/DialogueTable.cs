using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueTable", menuName = "YusData/DialogueTable")]
public class DialogueTable : YusTableSO<int, DialogueData>
{
    public override int GetKey(DialogueData data) => data.id;
}
