using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("閰嶇疆")]
    [SerializeField] private UIPanelDatabase panelDatabase; // 鎷栧叆 SO
    [SerializeField] private Transform uiRoot; // Canvas 涓嬬殑鎸傝浇鐐?

    [Header("Layer Roots (optional)")]
    [SerializeField] private bool autoCreateLayerRoots = true;
    [SerializeField] private Color modalBlockerColor = new Color(0, 0, 0, 0.6f);
    [SerializeField] private List<LayerRootEntry> layerRoots = new List<LayerRootEntry>();

    [Serializable]
    private class LayerRootEntry
    {
        public UILayer layer = UILayer.Popup;
        public Transform root;
    }

    private readonly Dictionary<string, BasePanel> panelCache = new Dictionary<string, BasePanel>();
    private readonly List<BasePanel> panelStack = new List<BasePanel>();
    private readonly Dictionary<UILayer, Transform> resolvedLayerRoots = new Dictionary<UILayer, Transform>();

    private class ModalBlocker
    {
        public readonly GameObject gameObject;
        public readonly RectTransform rectTransform;
        public readonly Button button;

        public ModalBlocker(GameObject gameObject, RectTransform rectTransform, Button button)
        {
            this.gameObject = gameObject;
            this.rectTransform = rectTransform;
            this.button = button;
        }
    }

    private readonly Dictionary<UILayer, ModalBlocker> modalBlockers = new Dictionary<UILayer, ModalBlocker>();

    private struct RuntimePanelOptions
    {
        public string panelName;
        public UILayer layer;
        public bool addToStack;
        public bool isModal;
        public bool closeOnBlockerClick;
        public bool destroyOnClose;
    }

    private readonly Dictionary<BasePanel, RuntimePanelOptions> runtimeOptionsByPanel =
        new Dictionary<BasePanel, RuntimePanelOptions>();

    private void Awake()
    {
        Instance = this;
        if (panelDatabase == null) YusLogger.Error("[UIManager] PanelDatabase is null!");
        if (uiRoot == null) YusLogger.Error("[UIManager] UIRoot is null!");

        BuildLayerRoots();
    }

    private void BuildLayerRoots()
    {
        resolvedLayerRoots.Clear();
        if (uiRoot == null) return;

        if (layerRoots != null)
        {
            foreach (var entry in layerRoots)
            {
                if (entry == null) continue;
                if (entry.root == null) continue;
                resolvedLayerRoots[entry.layer] = entry.root;
            }
        }

        if (!autoCreateLayerRoots) return;
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            if (resolvedLayerRoots.ContainsKey(layer)) continue;
            resolvedLayerRoots[layer] = CreateLayerRoot(layer);
        }
    }

    private Transform CreateLayerRoot(UILayer layer)
    {
        var go = new GameObject($"UI_{layer}");
        go.transform.SetParent(uiRoot, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return go.transform;
    }

    private Transform GetLayerRoot(UILayer layer)
    {
        if (uiRoot == null) return null;
        if (resolvedLayerRoots.Count == 0) BuildLayerRoots();

        if (resolvedLayerRoots.TryGetValue(layer, out var root) && root != null) return root;
        if (!autoCreateLayerRoots) return uiRoot;

        root = CreateLayerRoot(layer);
        resolvedLayerRoots[layer] = root;
        return root;
    }

    private RuntimePanelOptions ResolveOptions(string panelName)
    {
        var options = new RuntimePanelOptions
        {
            panelName = panelName,
            layer = UILayer.Popup,
            addToStack = true,
            isModal = false,
            closeOnBlockerClick = false,
            destroyOnClose = false,
        };

        if (panelDatabase != null && panelDatabase.TryGetEntry(panelName, out var entry))
        {
            options.layer = entry.layer;
            options.addToStack = entry.addToStack;
            options.isModal = entry.isModal;
            options.closeOnBlockerClick = entry.closeOnBlockerClick;
            options.destroyOnClose = entry.destroyOnClose;
        }

        return options;
    }

    private RuntimePanelOptions ApplyOverride(RuntimePanelOptions options, UIPanelOpenOptions overrideOptions)
    {
        options.layer = overrideOptions.layer;
        options.addToStack = overrideOptions.addToStack;
        options.isModal = overrideOptions.isModal;
        options.closeOnBlockerClick = overrideOptions.closeOnBlockerClick;
        options.destroyOnClose = overrideOptions.destroyOnClose;
        return options;
    }

    private ModalBlocker GetOrCreateModalBlocker(UILayer layer)
    {
        if (modalBlockers.TryGetValue(layer, out var existing) && existing != null && existing.gameObject != null)
        {
            return existing;
        }

        var root = GetLayerRoot(layer);
        if (root == null) return null;

        var go = new GameObject($"UI_{layer}_ModalBlocker");
        go.transform.SetParent(root, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var canvasGroup = go.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        var image = go.AddComponent<Image>();
        image.color = modalBlockerColor;
        image.raycastTarget = true;

        var button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        go.SetActive(false);

        var blocker = new ModalBlocker(go, rt, button);
        modalBlockers[layer] = blocker;
        return blocker;
    }

    private void DeactivateAllModalBlockers()
    {
        foreach (var kvp in modalBlockers)
        {
            if (kvp.Value?.gameObject == null) continue;
            kvp.Value.gameObject.SetActive(false);
            kvp.Value.button.onClick.RemoveAllListeners();
        }
    }

    private void RefreshModalBlocker()
    {
        BasePanel topModalPanel = null;
        RuntimePanelOptions topModalOptions = default;

        for (int i = panelStack.Count - 1; i >= 0; i--)
        {
            var panel = panelStack[i];
            if (panel == null) continue;
            if (!runtimeOptionsByPanel.TryGetValue(panel, out var options)) continue;
            if (!options.isModal) continue;

            topModalPanel = panel;
            topModalOptions = options;
            break;
        }

        if (topModalPanel == null)
        {
            DeactivateAllModalBlockers();
            return;
        }

        foreach (var kvp in modalBlockers)
        {
            if (kvp.Key == topModalOptions.layer) continue;
            if (kvp.Value?.gameObject == null) continue;
            kvp.Value.gameObject.SetActive(false);
            kvp.Value.button.onClick.RemoveAllListeners();
        }

        var blocker = GetOrCreateModalBlocker(topModalOptions.layer);
        if (blocker == null) return;

        blocker.button.onClick.RemoveAllListeners();
        if (topModalOptions.closeOnBlockerClick)
        {
            blocker.button.onClick.AddListener(CloseTopPanel);
        }

        blocker.gameObject.SetActive(true);
        blocker.rectTransform.SetAsLastSibling();
        topModalPanel.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 鎵撳紑闈㈡澘
    /// </summary>
    public T OpenPanel<T>(string panelName) where T : BasePanel
    {
        return OpenPanel<T>(panelName, useOverride: false, optionsOverride: default);
    }

    public T OpenPanel<T>(string panelName, UIPanelOpenOptions optionsOverride) where T : BasePanel
    {
        return OpenPanel<T>(panelName, useOverride: true, optionsOverride: optionsOverride);
    }

    private T OpenPanel<T>(string panelName, bool useOverride, UIPanelOpenOptions optionsOverride) where T : BasePanel
    {
        var options = ResolveOptions(panelName);
        if (useOverride) options = ApplyOverride(options, optionsOverride);

        BasePanel panel;
        if (panelCache.TryGetValue(panelName, out panel))
        {
            var root = GetLayerRoot(options.layer);
            if (root != null && panel.transform.parent != root)
            {
                panel.transform.SetParent(root, false);
            }
        }
        else
        {
            if (panelDatabase == null) return null;

            var prefab = panelDatabase.GetPrefab(panelName);
            if (prefab == null)
            {
                YusLogger.Error($"[UIManager] 鎵句笉鍒伴潰鏉块厤缃? {panelName}");
                return null;
            }

            var root = GetLayerRoot(options.layer);
            if (root == null) return null;

            panel = Instantiate(prefab, root);
            panel.Init();
            panelCache.Add(panelName, panel);
        }

        runtimeOptionsByPanel[panel] = options;

        if (options.addToStack)
        {
            if (panelStack.Contains(panel))
            {
                panelStack.Remove(panel);
            }

            if (panelStack.Count > 0)
            {
                panelStack[panelStack.Count - 1].OnPause();
            }

            panelStack.Add(panel);
        }

        panel.OpenWithTransition();
        panel.transform.SetAsLastSibling();
        RefreshModalBlocker();

        return panel as T;
    }

    /// <summary>
    /// 鍏抽棴褰撳墠椤跺眰闈㈡澘
    /// </summary>
    public void CloseTopPanel()
    {
        if (panelStack.Count == 0) return;

        var topPanel = panelStack[panelStack.Count - 1];
        panelStack.RemoveAt(panelStack.Count - 1);

        topPanel.CloseWithTransition(() =>
        {
            AfterPanelClosed(topPanel);

            if (panelStack.Count > 0)
            {
                panelStack[panelStack.Count - 1].OnResume();
            }

            RefreshModalBlocker();
        });
    }

    /// <summary>
    /// 鍏抽棴鎸囧畾闈㈡澘
    /// </summary>
    public void ClosePanel(string panelName)
    {
        if (!panelCache.TryGetValue(panelName, out var panel)) return;

        if (panelStack.Contains(panel))
        {
            if (panelStack[panelStack.Count - 1] == panel)
            {
                CloseTopPanel();
                return;
            }

            panelStack.Remove(panel);
            panel.CloseWithTransition(() =>
            {
                AfterPanelClosed(panel);
                RefreshModalBlocker();
            });
            return;
        }

        panel.CloseWithTransition(() => AfterPanelClosed(panel));
    }

    /// <summary>
    /// 鍏抽棴鎵€鏈夐潰鏉匡紙娓呮爤锛?
    /// </summary>
    public void CloseAllPanels()
    {
        CloseAllPanels(playTransition: false);
    }

    public void CloseAllPanels(bool playTransition)
    {
        for (int i = panelStack.Count - 1; i >= 0; i--)
        {
            var panel = panelStack[i];
            if (panel == null) continue;

            if (playTransition)
            {
                panel.CloseWithTransition(() => AfterPanelClosed(panel));
            }
            else
            {
                panel.Close();
                AfterPanelClosed(panel);
            }
        }

        panelStack.Clear();
        RefreshModalBlocker();
    }

    private void AfterPanelClosed(BasePanel panel)
    {
        if (panel == null) return;

        runtimeOptionsByPanel.TryGetValue(panel, out var options);

        bool shouldDestroy = options.destroyOnClose || !panel.isCacheable;
        if (!shouldDestroy) return;

        if (!string.IsNullOrEmpty(options.panelName))
        {
            if (panelCache.TryGetValue(options.panelName, out var cached) && cached == panel)
            {
                panelCache.Remove(options.panelName);
            }
        }

        runtimeOptionsByPanel.Remove(panel);
        Destroy(panel.gameObject);
    }

    /// <summary>
    /// 鑾峰彇闈㈡澘瀹炰緥锛堢敤浜庢洿鏂版暟鎹）
    /// </summary>
    public T GetPanel<T>(string panelName) where T : BasePanel
    {
        if (panelCache.TryGetValue(panelName, out var panel))
        {
            return panel as T;
        }
        return null;
    }
}

