using UnityEngine;
using System.Collections.Generic;

public class PoolSystemTest : MonoBehaviour
{
    // 资源路径 (对应 Resources/Test/MyCube)
    private const string PREFAB_PATH = "Test/MyCube";
    
    // 用来存手动生成的物体，方便手动回收测试
    private Stack<GameObject> activeObjects = new Stack<GameObject>();

    void OnGUI()
    {
        GUILayout.Label("=== 对象池测试面板 ===");
        GUILayout.Label("按 [Q]: 单个生成 (手动回收)");
        GUILayout.Label("按 [W]: 发射子弹 (2秒后自动回收)");
        GUILayout.Label("按 [E]: 回收最近一个 (Release)");
        GUILayout.Label("按 [R]: 疯狂连发 (压力测试)");
        GUILayout.Label($"当前活跃手动对象: {activeObjects.Count}");
    }

    void Update()
    {
        // 1. 手动生成测试 (Get)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 从池中获取
            GameObject obj = YusPoolManager.Instance.Get(PREFAB_PATH);
            
            // 设置位置 (随机一点)
            obj.transform.position = Random.insideUnitSphere * 3f + Vector3.up * 2f;
            
            // 记录下来
            activeObjects.Push(obj);
        }

        // 2. 自动回收测试 (ReturnToPool)
        if (Input.GetKeyDown(KeyCode.W))
        {
            GameObject obj = YusPoolManager.Instance.Get(PREFAB_PATH);
            obj.transform.position = transform.position; // 从当前脚本位置发射
            
            // 利用 PoolObject 组件的扩展方法，2秒后自动回收
            obj.GetComponent<PoolObject>().ReturnToPool(2.0f);
            
            Debug.Log("发射了一颗 2秒后消失的子弹");
        }

        // 3. 手动回收测试 (Release)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (activeObjects.Count > 0)
            {
                GameObject topObj = activeObjects.Pop();
                
                // 归还给池子
                YusPoolManager.Instance.Release(topObj);
            }
            else
            {
                Debug.LogWarning("没有可回收的对象了！");
            }
        }

        // 4. 压力测试
        if (Input.GetKey(KeyCode.R))
        {
            // 每帧发射 5 个
            for (int i = 0; i < 5; i++)
            {
                GameObject obj = YusPoolManager.Instance.Get(PREFAB_PATH);
                obj.transform.position = transform.position + Random.insideUnitSphere;
                obj.GetComponent<PoolObject>().ReturnToPool(1.0f); // 1秒后回收
            }
        }
    }
}