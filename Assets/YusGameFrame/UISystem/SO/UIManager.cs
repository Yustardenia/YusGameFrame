using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("配置")]
    [SerializeField] private UIPanelDatabase panelDatabase; // 拖入刚才做的SO
    [SerializeField] private Transform uiRoot; // Canvas 下的挂载点

    // 缓存池：<面板名称, 实例化的面板>
    private Dictionary<string, BasePanel> panelCache = new Dictionary<string, BasePanel>();

    // 页面栈：用于管理层级和返回键
    private List<BasePanel> panelStack = new List<BasePanel>();

    private void Awake()
    {
        Instance = this;
        if (panelDatabase == null) YusLogger.Error("[UIManager] PanelDatabase is null!");
        if (uiRoot == null) YusLogger.Error("[UIManager] UIRoot is null!");
    }

    /// <summary>
    /// 打开面板
    /// </summary>
    public T OpenPanel<T>(string panelName) where T : BasePanel
    {
        // 1. 查找缓存
        BasePanel panel = null;
        if (panelCache.TryGetValue(panelName, out panel))
        {
            panel.Open();
        }
        else
        {
            // 2. 缓存没有，去数据库加载 Prefab
            if (panelDatabase == null || uiRoot == null) return null;

            BasePanel prefab = panelDatabase.GetPrefab(panelName);
            if (prefab != null)
            {
                panel = Instantiate(prefab, uiRoot);
                panel.Init(); // 初始化
                panelCache.Add(panelName, panel);
                panel.Open();
            }
            else
            {
                YusLogger.Error($"[UIManager] 找不到面板配置: {panelName}");
                return null;
            }
        }

        // 3. 入栈管理
        // 如果栈中已有该面板，先移除，避免重复入栈
        if (panelStack.Contains(panel))
        {
            panelStack.Remove(panel);
        }

        // 暂停上一个面板
        if (panelStack.Count > 0)
        {
            panelStack[panelStack.Count - 1].OnPause();
        }
        panelStack.Add(panel);

        return panel as T;
    }

    /// <summary>
    /// 关闭当前顶层面板
    /// </summary>
    public void CloseTopPanel()
    {
        if (panelStack.Count == 0) return;

        BasePanel topPanel = panelStack[panelStack.Count - 1];
        panelStack.RemoveAt(panelStack.Count - 1);
        topPanel.Close();

        // 恢复上一个面板（如果有）
        if (panelStack.Count > 0)
        {
            BasePanel nextPanel = panelStack[panelStack.Count - 1];
            nextPanel.OnResume();
        }
    }

    /// <summary>
    /// 关闭指定面板
    /// </summary>
    public void ClosePanel(string panelName)
    {
        if (panelCache.TryGetValue(panelName, out BasePanel panel))
        {
            if (panelStack.Contains(panel))
            {
                if (panelStack[panelStack.Count - 1] == panel)
                {
                    CloseTopPanel();
                }
                else
                {
                    panelStack.Remove(panel);
                    panel.Close();
                }
            }
            else
            {
                panel.Close();
            }
        }
    }

    /// <summary>
    /// 关闭所有面板（清栈）
    /// </summary>
    public void CloseAllPanels()
    {
        for (int i = panelStack.Count - 1; i >= 0; i--)
        {
            panelStack[i].Close();
        }
        panelStack.Clear();
    }

    /// <summary>
    /// 获取面板实例（用于更新数据）
    /// </summary>
    public T GetPanel<T>(string panelName) where T : BasePanel
    {
        if (panelCache.TryGetValue(panelName, out BasePanel panel))
        {
            return panel as T;
        }
        return null;
    }
}