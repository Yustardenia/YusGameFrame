public interface IState
{
    // 进入状态时调用（只一次）
    void OnEnter();

    // 每帧调用 (Update)
    void OnUpdate();

    // 物理帧调用 (FixedUpdate)
    void OnFixedUpdate();

    // 退出状态时调用（只一次）
    void OnExit();
}