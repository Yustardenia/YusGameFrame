using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocalizationTable", menuName = "YusData/LocalizationTable")]
public class LocalizationTable : YusTableSO<string, LocalizationData>
{
    public override string GetKey(LocalizationData data) => data.key;
}
