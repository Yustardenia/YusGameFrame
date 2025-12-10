using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class YusDataManager
{
    private static YusDataManager instance = new YusDataManager();
    public static YusDataManager Instance => instance;

    // 配置表缓存
    private Dictionary<string, ScriptableObject> tableCache = new Dictionary<string, ScriptableObject>();

    // 存档路径 (PersistentDataPath)
    public static string SAVE_PATH => Application.persistentDataPath + "/SaveData/";

    private YusDataManager() { }

    // ================= 配置表部分 (Static Config) =================

    /// <summary>
    /// 获取配置表 (自动加载 SO)
    /// </summary>
    public TContainer GetConfig<TContainer>() where TContainer : ScriptableObject
    {
        string name = typeof(TContainer).Name;
        if (tableCache.ContainsKey(name)) return tableCache[name] as TContainer;

        // 假设生成的 SO 放在 Resources/YusData/ 下
        // 生产环境建议改为 Addressables.LoadAssetAsync
        TContainer table = Resources.Load<TContainer>($"YusData/{name}");
        
        if (table != null)
        {
            // 反射调用一次 Init，或者让 TContainer 实现特定初始化接口
            var initMethod = typeof(TContainer).GetMethod("Init");
            initMethod?.Invoke(table, null);
            tableCache[name] = table;
        }
        else
        {
            Debug.LogError($"[Yus] 未找到配置表: Resources/YusData/{name}");
        }

        return table;
    }

    // ================= 存档部分 (Dynamic Save) =================

    /// <summary>
    /// 保存列表数据到二进制 (极速模式)
    /// </summary>
    public void SaveList<TData>(List<TData> list, string fileName) where TData : IYusBinaryData
    {
        try
        {
            if (!Directory.Exists(SAVE_PATH)) Directory.CreateDirectory(SAVE_PATH);
            string path = Path.Combine(SAVE_PATH, fileName + ".yus");

            using (var fs = File.Open(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write("YUS_SAVE"); // Magic
                bw.Write(1);          // Version
                bw.Write(list.Count); // Count
                
                foreach (var item in list)
                {
                    item.Write(bw); // 调用接口方法，无反射
                }
            }
            Debug.Log($"[Yus] 保存成功: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Yus] 保存失败: {e.Message}");
        }
    }

    /// <summary>
    /// 从二进制读取列表数据
    /// </summary>
    public List<TData> LoadList<TData>(string fileName) where TData : IYusBinaryData, new()
    {
        List<TData> list = new List<TData>();
        string path = Path.Combine(SAVE_PATH, fileName + ".yus");

        if (!File.Exists(path)) return list;

        try
        {
            using (var fs = File.Open(path, FileMode.Open))
            using (var br = new BinaryReader(fs))
            {
                if (br.ReadString() != "YUS_SAVE") return list;
                int version = br.ReadInt32();
                int count = br.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    TData data = new TData();
                    data.Read(br); // 调用接口方法，无反射
                    list.Add(data);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Yus] 读取失败: {e.Message}");
        }
        return list;
    }
    /// <summary>
    /// [编辑器专用] 将当前数据反写回 Excel
    /// </summary>
    public void SaveListToExcel<TData>(List<TData> list)
    {
#if UNITY_EDITOR
        // 使用反射调用 Editor 代码，避免打包报错
        string editorAssembly = "Assembly-CSharp-Editor";
        System.Reflection.Assembly assembly = null;
        try 
        {
            assembly = System.Reflection.Assembly.Load(editorAssembly);
        }
        catch 
        {
            // 如果 Editor 代码也在 Assembly-CSharp 中 (未分程序集)
            assembly = System.Reflection.Assembly.GetExecutingAssembly();
        }

        System.Type writerType = assembly?.GetType("ExcelYusWriter");
        
        if (writerType != null)
        {
            var method = writerType.GetMethod("WriteBack").MakeGenericMethod(typeof(TData));
            method.Invoke(null, new object[] { list });
        }
        else
        {
            Debug.LogError("未找到 ExcelYusWriter，请确保文件在 Editor 目录下");
        }
#else
        Debug.LogError("反写 Excel 功能仅在 Unity 编辑器模式下可用！");
#endif
    }
    
    /// <summary>
    /// [核心功能] 修正版：支持任意 Key 类型 (int/string)
    /// </summary>
    public List<TData> CreateRuntimeListFromConfig<TTable, TData>() 
        where TTable : ScriptableObject // 放宽约束，内部再转换
        where TData : IYusCloneable<TData>
    {
        // 1. 获取静态配置
        TTable table = GetConfig<TTable>();
        if (table == null) return new List<TData>();

        // 2. 反射获取 GetAll 方法 (因为 TTable 被放宽为 ScriptableObject 了)
        // 这样可以兼容 YusTableSO<int, TData> 也可以兼容 YusTableSO<string, TData>
        var method = table.GetType().GetMethod("GetAll");
        if (method == null) return new List<TData>();

        List<TData> originalList = method.Invoke(table, null) as List<TData>;
        if (originalList == null) return new List<TData>();

        // 3. 克隆
        List<TData> runtimeList = new List<TData>(originalList.Count);
        foreach (var item in originalList)
        {
            runtimeList.Add(item.Clone());
        }

        return runtimeList;
    }

    /// <summary>
    /// [辅助功能] 基于 ID，创建一个全新的、独立的单条运行时数据
    /// </summary>
    public TData CreateRuntimeInstance<TTable, TData>(int id) 
        where TTable : YusTableSO<int, TData>
        where TData : IYusCloneable<TData>
    {
        TTable table = GetConfig<TTable>();
        if (table == null) return default;

        TData configData = table.Get(id);
        if (configData == null) return default;

        // 返回克隆体
        return configData.Clone();
    }
}