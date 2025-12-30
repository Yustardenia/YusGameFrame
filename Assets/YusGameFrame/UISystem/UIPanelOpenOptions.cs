using System;

[Serializable]
public struct UIPanelOpenOptions
{
    public UILayer layer;
    public bool addToStack;
    public bool isModal;
    public bool closeOnBlockerClick;
    public bool destroyOnClose;

    public static UIPanelOpenOptions Default => new UIPanelOpenOptions
    {
        layer = UILayer.Popup,
        addToStack = true,
        isModal = false,
        closeOnBlockerClick = false,
        destroyOnClose = false,
    };
}

