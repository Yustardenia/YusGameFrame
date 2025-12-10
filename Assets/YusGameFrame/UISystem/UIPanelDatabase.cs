using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/PanelDatabase")]
public class UIPanelDatabase : ScriptableObject
{
    [System.Serializable]
    public class PanelEntry
    {
        public string panelName;  // 或者使用 enum
        public BasePanel panelPrefab;
    }

    public List<PanelEntry> entries;

    public BasePanel GetPrefab(string name)
    {
        var entry = entries.Find(x => x.panelName == name);
        return entry != null ? entry.panelPrefab : null;
    }
}