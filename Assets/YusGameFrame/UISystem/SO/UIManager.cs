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
    private Stack<BasePanel> panelStack = new Stack<BasePanel>();

    private void Awake()
    {
        Instance = this;
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

        // 3. 入栈管理（可选：如果打开的是全屏面板，可能需要把上一个面板暂停）
        if (panelStack.Count > 0)
        {
            // 可以在这里让上一个面板 OnPause()，看需求
        }
        panelStack.Push(panel);

        return panel as T;
    }

    /// <summary>
    /// 关闭当前顶层面板
    /// </summary>
    public void CloseTopPanel()
    {
        if (panelStack.Count == 0) return;

        BasePanel topPanel = panelStack.Pop();
        topPanel.Close();

        // 恢复上一个面板（如果有）
        if (panelStack.Count > 0)
        {
            BasePanel nextPanel = panelStack.Peek();
            // nextPanel.OnResume();
        }
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