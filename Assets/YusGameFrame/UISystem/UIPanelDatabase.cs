using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "UI/PanelDatabase")]
public class UIPanelDatabase : ScriptableObject
{
    [System.Serializable]
    public class PanelEntry
    {
        public string panelName;  // 鎴栬€呬娇鐢?enum

        [FormerlySerializedAs("panelPrefab")]
        public BasePanel panelPrefab;

        public UILayer layer = UILayer.Popup;
        public bool addToStack = true;
        public bool isModal = false;
        public bool closeOnBlockerClick = false;
        public bool destroyOnClose = false;
    }

    public List<PanelEntry> entries;

    public BasePanel GetPrefab(string name)
    {
        var entry = entries?.Find(x => x.panelName == name);
        return entry != null ? entry.panelPrefab : null;
    }

    public bool TryGetEntry(string name, out PanelEntry entry)
    {
        entry = entries?.Find(x => x.panelName == name);
        return entry != null;
    }
}

