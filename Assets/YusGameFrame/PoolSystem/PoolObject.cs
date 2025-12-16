using UnityEngine;
using System.Collections;

public class PoolObject : MonoBehaviour
{
    // 归属的池子名称(通常是 Prefab 路径)
    public string PoolName { get; private set; }

    // 是否正在使用中
    public bool IsInUse { get; private set; }

    public void Init(string poolName)
    {
        this.PoolName = poolName;
    }

    public void OnSpawn()
    {
        IsInUse = true;
        // 触发接口逻辑
        var poolables = GetComponents<IPoolable>();
        foreach (var p in poolables) p.OnSpawn();
    }

    public void OnRecycle()
    {
        IsInUse = false;
        YusCoroutine.StopOwner(this); // 停止所有绑定到本对象的协程，防止回收后逻辑还在跑

        var poolables = GetComponents<IPoolable>();
        foreach (var p in poolables) p.OnRecycle();
    }

    // --- 实用功能：延迟回收 ---
    public void ReturnToPool(float delay = 0f)
    {
        if (delay > 0)
        {
            YusCoroutine.Delay(delay, () => YusPoolManager.Instance.Release(this.gameObject), this, tag: "PoolObject.ReturnToPool");
        }
        else
        {
            YusPoolManager.Instance.Release(this.gameObject);
        }
    }
}
