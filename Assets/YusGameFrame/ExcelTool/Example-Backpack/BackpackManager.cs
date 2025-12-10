using UnityEngine;

public class BackpackManager : YusBaseManager<BackpackTable, BackpackData>
{
    // --- 1. 添加单例代码 (方便其他脚本调用) ---
    public static BackpackManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    // ---------------------------------------

    protected override string SaveFileName => "MyBackpackSave";

    // 业务逻辑：使用物品
    public void UseItem(int itemId)
    {
        var item = DataList.Find(x => x.id == itemId); // 使用 Lambda 查找
        if (item != null)
        {
            Debug.Log($"使用了: {item.name}, 剩余耐久: {item.durability}");
            item.durability -= 10;
            if (item.durability < 0) item.durability = 0;
            
            Save(); // 自动保存
            Dev_WriteBackToExcel();
        }
    }
}