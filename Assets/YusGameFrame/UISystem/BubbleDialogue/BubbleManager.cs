using UnityEngine;
using System.Linq;

// 1. 继承基类，泛型填 BubbleTable 和 BubbleData
public class BubbleManager : YusBaseManager<BubbleDialogueTable, BubbleDialogueData>
{
    // 单例
    public static BubbleManager Instance { get; private set; }
    private void Awake() 
    { 
        Instance = this; 
    }

    protected override string SaveFileName => "BubbleHistorySave"; // 存档文件名

    // 事件：当有新气泡添加时通知 UI，带上新添加的那条数据
    public event System.Action<BubbleDialogueData> OnBubbleAdded;
    
    // 事件：当历史记录加载完毕（重置）时通知 UI
    public event System.Action OnHistoryLoaded;

    // --- 重写基类回调 ---
    protected override void OnLoadSuccess(bool isNewGame)
    {
        base.OnLoadSuccess(isNewGame);
        // 数据加载完成后，通知 UI 刷新整个列表
        OnHistoryLoaded?.Invoke();
    }

    // --- 业务逻辑 ---

    /// <summary>
    /// 添加气泡 (Fungus 调用)
    /// </summary>
    /// <param name="key">ID (如果存在则跳过)</param>
    /// <param name="text">内容 (如果是新建)</param>
    public void AddBubble(int key, string text, string name, bool isRight)
    {
        // 1. 检查 ID 是否已存在 (防止重复触发)
        if (DataList.Any(x => x.id == key))
        {
            Debug.Log($"[BubbleManager] ID {key} 已存在，跳过。");
            return;
        }

        // 2. 尝试从 Excel 配置表里克隆 (如果有配置的话)
        BubbleDialogueTable configTable = YusDataManager.Instance.GetConfig<BubbleDialogueTable>();
        BubbleDialogueData newData = null;
        
        // 尝试从配置表找预设数据
        if (configTable != null)
        {
            var configData = configTable.Get(key);
            if (configData != null) newData = configData.Clone();
        }

        // 3. 如果没配置，则手动创建 (动态对话)
        if (newData == null)
        {
            newData = new BubbleDialogueData
            {
                id = key,
                text = text,
                textName = name,
                isRight = isRight
            };
        }

        // 4. 加入列表并保存
        DataList.Add(newData);
        Save();

        // 5. 通知 UI 表现
        OnBubbleAdded?.Invoke(newData);
    }

    /// <summary>
    /// 检查某条对话是否已触发 (用于 GenerateButtonContainer 判断)
    /// </summary>
    public bool HasDialogue(int id)
    {
        return DataList.Any(x => x.id == id);
    }
    
    /// <summary>
    /// 获取下一个可用 ID (自动递增逻辑)
    /// </summary>
    public int GetNextId()
    {
        if (DataList.Count == 0) return 1;
        return DataList.Max(x => x.id) + 1;
    }
}