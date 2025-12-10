using UnityEngine;
using System.Collections.Generic;

public abstract class YusTableSO<TKey, TData> : ScriptableObject
{
    [SerializeField]
    protected List<TData> dataList = new List<TData>();

    // 运行时查找字典
    protected Dictionary<TKey, TData> dataDic;

    /// <summary>
    /// 初始化字典 (只需调用一次)
    /// </summary>
    public virtual void Init()
    {
        if (dataDic != null) return;
        dataDic = new Dictionary<TKey, TData>();

        foreach (var data in dataList)
        {
            TKey key = GetKey(data);
            if (!dataDic.ContainsKey(key))
            {
                dataDic.Add(key, data);
            }
        }
    }

    public TData Get(TKey key)
    {
        if (dataDic == null) Init();
        if (dataDic.TryGetValue(key, out TData val)) return val;
        return default;
    }

    public List<TData> GetAll() => dataList;

    public abstract TKey GetKey(TData data);

#if UNITY_EDITOR
    public void EditorSetData(List<TData> list) => dataList = list;
#endif
}