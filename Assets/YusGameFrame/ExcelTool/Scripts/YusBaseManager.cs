using UnityEngine;
using System.Collections.Generic;
using System.Reflection; // 需要反射来做通用处理

public abstract class YusBaseManager<TTable, TData> : MonoBehaviour
    where TTable : ScriptableObject 
    where TData : IYusBinaryData, IYusCloneable<TData>, new()
{
    public List<TData> DataList = new List<TData>();
    protected abstract string SaveFileName { get; }

    protected virtual void Start()
    {
        InitData();
    }

    public virtual void InitData()
    {
        // 1. 尝试读档
        var savedData = YusDataManager.Instance.LoadList<TData>(SaveFileName);

        if (savedData != null && savedData.Count > 0)
        {
            // === [关键修改点] 读档成功后，图片是空的，需要从配置表里找回来 ===
            RelinkAssets(savedData);
            // ==========================================================

            DataList = savedData;
            OnLoadSuccess(false);
            Debug.Log($"[{this.GetType().Name}] 读档成功 (已重连资源)，数量: {DataList.Count}");
        }
        else
        {
            // 2. 新游戏，直接克隆配置
            DataList = YusDataManager.Instance.CreateRuntimeListFromConfig<TTable, TData>();
            OnLoadSuccess(true);
            Debug.Log($"[{this.GetType().Name}] 新游戏初始化");
            Save();
        }
    }

    /// <summary>
    /// [修复图片丢失的核心方法] 资源重连
    /// 原理：存档只有数值，配置表有图片。
    /// 我们遍历存档，用 ID 去配置表找“原件”，把“原件”里的图片赋给存档。
    /// </summary>
  /// <summary>
    /// [修复图片丢失的核心方法] 资源重连 (反射版，兼容性更好)
    /// </summary>
    private void RelinkAssets(List<TData> runtimeList)
    {
        // 1. 获取配置表
        TTable table = YusDataManager.Instance.GetConfig<TTable>();
        if (table == null) return;

        // 2. 找出 TData 中所有的资源字段 (Sprite, GameObject, Texture...)
        var fields = typeof(TData).GetFields();
        var assetFields = new List<FieldInfo>();
        foreach (var f in fields)
        {
            // 如果字段类型是 UnityEngine.Object 或其子类 (如 Sprite)
            if (typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
            {
                assetFields.Add(f);
            }
        }

        if (assetFields.Count == 0) return;

        // --- 核心修改：使用反射替代 dynamic ---
        
        // 获取配置表的类型
        System.Type tableType = table.GetType();
        
        // 反射获取 GetKey 和 Get 方法
        // 注意：因为 YusTableSO 是泛型基类，直接 GetMethod 可能需要注意，但通常 GetMethod("Name") 对于公共方法是有效的
        MethodInfo getKeyMethod = tableType.GetMethod("GetKey");
        MethodInfo getMethod = tableType.GetMethod("Get");

        if (getKeyMethod == null || getMethod == null)
        {
            Debug.LogError($"[{this.GetType().Name}] 无法通过反射找到 GetKey 或 Get 方法，请检查 Table 类定义。");
            return;
        }

        // 复用参数数组以减少 GC (可选)
        object[] getKeyParams = new object[1];
        object[] getParams = new object[1];

        foreach (var runtimeItem in runtimeList)
        {
            try
            {
                // A. 反射调用 GetKey(runtimeItem)
                getKeyParams[0] = runtimeItem;
                object key = getKeyMethod.Invoke(table, getKeyParams);

                // B. 反射调用 Get(key)
                getParams[0] = key;
                object configItem = getMethod.Invoke(table, getParams);

                if (configItem != null)
                {
                    // C. 把配置表里的“图片/模型”，赋值给存档数据
                    foreach (var field in assetFields)
                    {
                        object assetRef = field.GetValue(configItem); // 拿原本的图片
                        field.SetValue(runtimeItem, assetRef);        // 塞给没图片的存档
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"重连资源失败: {e.Message}");
            }
        }
    }

    // ... 下面的 Save, Dev_WriteBackToExcel 等保持不变 ...
    public virtual void Save()
    {
        YusDataManager.Instance.SaveList(DataList, SaveFileName);
    }

    [ContextMenu("开发者/反写回 Excel")]
    public void Dev_WriteBackToExcel()
    {
        YusDataManager.Instance.SaveListToExcel(DataList);
    }
    
    [ContextMenu("开发者/重置存档")]
    public void Dev_ResetSave()
    {
        string path = System.IO.Path.Combine(YusDataManager.SAVE_PATH, SaveFileName + ".yus");
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        InitData();
    }

    protected virtual void OnLoadSuccess(bool isNewGame) { }
}