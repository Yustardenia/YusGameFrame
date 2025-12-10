using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BackpackTable", menuName = "YusData/BackpackTable")]
public class BackpackTable : YusTableSO<int, BackpackData>
{
    public override int GetKey(BackpackData data) => data.id;
}
