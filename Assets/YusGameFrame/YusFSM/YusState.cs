public abstract class YusState<T> : IState
{
    protected T owner;      // 持有者（谁拥有这个状态）
    protected YusFSM<T> fsm; // 所属的状态机

    // 构造函数注入
    public void Init(T owner, YusFSM<T> fsm)
    {
        this.owner = owner;
        this.fsm = fsm;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnExit() { }
}