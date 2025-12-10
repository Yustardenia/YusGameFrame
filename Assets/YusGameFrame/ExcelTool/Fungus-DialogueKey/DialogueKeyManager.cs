using UnityEngine;
using System.Collections.Generic;

// 1. 继承泛型基类
public class DialogueKeyManager : YusBaseManager<DialogueTable, DialogueData>
{
    // --- 单例模式 (由 YusSingletonManager 统一管理) ---
    public static DialogueKeyManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        
        // 基类的 Start 会调用 InitData
    }

    // 2. 定义存档文件名
    protected override string SaveFileName => "DialogueKeySave";

    // 3. 查找缓存 (优化性能，避免每次 Find 都遍历 List)
    private Dictionary<int, DialogueData> _lookupCache = new Dictionary<int, DialogueData>();

    // 重写加载成功回调：构建缓存
    protected override void OnLoadSuccess(bool isNewGame)
    {
        _lookupCache.Clear();
        foreach (var data in DataList)
        {
            if (!_lookupCache.ContainsKey(data.id))
            {
                _lookupCache.Add(data.id, data);
            }
        }
        Debug.Log($"[DialogueKeyManager] 就绪，加载了 {DataList.Count} 条对话数据。");
    }

    // --- 业务逻辑 API ---

    public DialogueData GetDialogue(int id)
    {
        if (_lookupCache.TryGetValue(id, out var data))
        {
            return data;
        }
        return null;
    }

    public bool SetCanTrigger(int id, bool canTrigger)
    {
        var dlg = GetDialogue(id);
        if (dlg == null) return false;

        if (dlg.canTrigger != canTrigger)
        {
            dlg.canTrigger = canTrigger;
            Save(); // 状态改变，自动存档
        }
        return true;
    }

    public bool IncrementTriggerCount(int id)
    {
        var dlg = GetDialogue(id);
        if (dlg == null) return false;

        dlg.triggerCount++;
        
        /*// 解析 extra 检查最大次数逻辑 (保留你的逻辑)
        // 假设 extra 格式: "maxTriggers=3"
        int max = GetMaxTriggers(dlg.triggerConditionName);
        if (max != -1 && dlg.triggerCount >= max)
        {
            dlg.canTrigger = false;
        }*/

        Save(); // 数值改变，自动存档
        Dev_WriteBackToExcel();
        return true;
    }

    // 动态添加对话键 (支持运行时生成新 Key)
    public bool AddDynamicDialogue(int newId, int npcId, string text, bool initialCanTrigger = true)
    {
        if (_lookupCache.ContainsKey(newId))
        {
            Debug.LogWarning($"ID {newId} 已存在，无法添加");
            return false;
        }

        // 创建新数据 (纯内存操作)
        var newData = new DialogueData
        {
            id = newId,
            npcId = npcId,
            dialogueText = text,
            canTrigger = initialCanTrigger,
            triggerCount = 0,
            triggerConditionName = ""
        };

        // 加入列表和缓存
        DataList.Add(newData);
        _lookupCache.Add(newId, newData);

        Save(); // 保存新结构
        Dev_WriteBackToExcel();
        return true;
    }

    // --- 辅助方法 ---
    /*private int GetMaxTriggers(string extra)
    {
        if (string.IsNullOrEmpty(extra)) return -1;
        // 简单解析示例，实际可用 Split 等
        if (extra.Contains("maxTriggers="))
        {
            string val = extra.Split('=')[1];
            if (int.TryParse(val, out int res)) return res;
        }
        return -1;
    }*/
}