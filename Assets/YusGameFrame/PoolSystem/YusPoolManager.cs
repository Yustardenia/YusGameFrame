using UnityEngine;
using System.Collections.Generic;

public class YusPoolManager : MonoBehaviour
{
    public static YusPoolManager Instance { get; private set; }

    // 根节点，用来整理 Hierarchy，不让场景太乱
    private Transform poolRoot;
    // 具体的池子容器：Key=路径, Value=该路径对应的对象队列
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    // 池子父节点缓存：Key=路径, Value=Hierarchy里的父物体
    private Dictionary<string, Transform> poolParents = new Dictionary<string, Transform>();

    private void Awake()
    {
        Instance = this;
        // 创建一个子物体作为池子根节点，保持 Hierarchy 整洁
        poolRoot = new GameObject("=== PoolRoot ===").transform;
        poolRoot.SetParent(this.transform);
    }

    // ==========================================
    // 1. 取出对象 (Get)
    // ==========================================
    
    /// <summary>
    /// 获取对象（如果没有则通过 ResManager 加载）
    /// </summary>
    /// <param name="path">资源路径 (传给 YusResManager 的路径)</param>
    /// <param name="parent">可选父节点</param>
    public GameObject Get(string path, Transform parent = null)
    {
        GameObject obj = null;

        // A. 检查池子里有没有
        if (poolDict.TryGetValue(path, out Queue<GameObject> queue) && queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            // B. 池子空了/不存在，加载新对象
            // 使用你的 YusResManager 加载 Prefab
            GameObject prefab = YusResManager.Instance.Load<GameObject>(path);
            if (prefab == null)
            {
                YusLogger.Error($"[YusPool] 无法加载 Prefab: {path}");
                return null;
            }
            
            obj = Instantiate(prefab);
            // 加上标记组件，记住所属池子
            var flag = obj.AddComponent<PoolObject>(); 
            flag.Init(path);
        }

        // C. 设置状态
        // 这一步很重要：要在 SetParent 之前 SetActive，或者之后，取决于 OnEnable 的逻辑
        // 推荐：先归位，再激活
        obj.transform.SetParent(parent); 
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.SetActive(true);

        // 触发 OnSpawn
        obj.GetComponent<PoolObject>().OnSpawn();

        return obj;
    }

    // ==========================================
    // 2. 归还对象 (Release)
    // ==========================================

    public void Release(GameObject obj)
    {
        if (obj == null) return;

        // 获取标记组件
        PoolObject flag = obj.GetComponent<PoolObject>();
        if (flag == null)
        {
            // 如果没有标记，说明不是从池子拿的，直接销毁
            YusLogger.Warning($"[YusPool] 对象 {obj.name} 没有 PoolObject 组件，直接销毁。");
            Destroy(obj);
            return;
        }

        if (!flag.IsInUse) return; // 防止重复回收

        string path = flag.PoolName;

        // 触发 OnRecycle
        flag.OnRecycle();

        // 隐藏
        obj.SetActive(false);

        // 整理 Hierarchy：放回对应的父节点下
        if (!poolParents.TryGetValue(path, out Transform poolParent))
        {
            // 创建子池子节点
            GameObject group = new GameObject($"Pool_{path.Replace("/", "_")}");
            group.transform.SetParent(poolRoot);
            poolParent = group.transform;
            poolParents[path] = poolParent;
        }
        obj.transform.SetParent(poolParent);

        // 入队
        if (!poolDict.ContainsKey(path))
        {
            poolDict[path] = new Queue<GameObject>();
        }
        poolDict[path].Enqueue(obj);
    }

    // ==========================================
    // 4. 预热 (Prewarm)
    // ==========================================

    /// <summary>
    /// 预热对象池
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="count">预热数量</param>
    public void Prewarm(string path, int count)
    {
        if (count <= 0) return;

        // 1. 确保池子结构存在
        if (!poolDict.ContainsKey(path))
        {
            poolDict[path] = new Queue<GameObject>();
        }

        // 2. 准备父节点
        if (!poolParents.TryGetValue(path, out Transform poolParent))
        {
            GameObject group = new GameObject($"Pool_{path.Replace("/", "_")}");
            group.transform.SetParent(poolRoot);
            poolParent = group.transform;
            poolParents[path] = poolParent;
        }

        // 3. 加载 Prefab (只加载一次)
        GameObject prefab = YusResManager.Instance.Load<GameObject>(path);
        if (prefab == null)
        {
            YusLogger.Error($"[YusPool] 预热失败，无法加载 Prefab: {path}");
            return;
        }

        // 4. 循环生成
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            
            // 初始化标记
            var flag = obj.AddComponent<PoolObject>();
            flag.Init(path);

            // 设置状态：隐藏、归位
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            obj.transform.localPosition = Vector3.zero; 

            // 入队
            poolDict[path].Enqueue(obj);
        }
        
        YusLogger.Log($"[YusPool] 预热完成: {path}, 数量: {count}");
    }

    // ==========================================
    // 3. 清理 (Clear)
    // ==========================================
    
    // 切换场景时通常不需要手动清理，因为 DontDestroyOnLoad
    // 但如果内存吃紧，可以调用这个
    public void ClearAll()
    {
        poolDict.Clear();
        poolParents.Clear();
        // 销毁所有缓存的物体
        foreach (Transform child in poolRoot)
        {
            Destroy(child.gameObject);
        }
    }
#if UNITY_EDITOR
    /// <summary>
    /// 获取内部池子字典
    /// </summary>
    public Dictionary<string, Queue<GameObject>> Debug_GetPoolDict()
    {
        return poolDict;
    }

    /// <summary>
    /// 获取池子根节点
    /// </summary>
    public Transform Debug_GetRoot()
    {
        return poolRoot;
    }
#endif
}