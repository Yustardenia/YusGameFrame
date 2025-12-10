using UnityEngine;

// 挂载在 Prefab 上，用于验证 IPoolable 接口是否生效
public class TestPoolItem : MonoBehaviour, IPoolable
{
    private Rigidbody rb;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        
    }

    // --- 接口实现 ---

    // 1. 当从池子取出时 (相当于 Start)
    public void OnSpawn()
    {
        // 随机变个颜色，证明这是新的一次生命
        if (meshRenderer) meshRenderer.material.color = Random.ColorHSV();
        
        // 重置物理速度 (防止回收时带有残留速度)
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // 给一个向上的力，模拟弹射
            rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }

        Debug.Log($"<color=green>[Item] {name} 已生成 (OnSpawn)</color>");
    }

    // 2. 当归还给池子时 (相当于 OnDisable)
    public void OnRecycle()
    {
        Debug.Log($"<color=yellow>[Item] {name} 已回收 (OnRecycle)</color>");
    }

    // --- 模拟业务逻辑 ---
    void Update()
    {
        // 简单旋转
        transform.Rotate(Vector3.up * 90 * Time.deltaTime);
    }
}