public interface IPoolable
{
    // 当从池中取出时调用 (相当于 Start/OnEnable)
    void OnSpawn();

    // 当归还给池子时调用 (相当于 OnDisable/OnDestroy)
    void OnRecycle();
}