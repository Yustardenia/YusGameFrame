using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 自动生成的输入控制器
/// [注意事项]:
/// 1. 持续性动作(移动)应读取输入缓存，逻辑放入 FixedUpdate (物理) 或 Update
/// 2. 瞬发类动作(跳跃/攻击)可在回调中直接写逻辑
/// 3. 对话/过场时，请调用 YusInputManager.Instance.EnableUI() 锁住操作
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Input Cache")]
    [SerializeField] private Vector2 _inputMove;

    void Start()
    {
        // 自动注册输入事件 (物体销毁自动解绑)
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Move, OnMove);
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Jump, OnJump);
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Fire, OnFire);
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Dash, OnDash);
    }

    void Update()
    {
        // TODO: 处理非物理逻辑 (如动画状态机参数更新)
    }

    void FixedUpdate()
    {
        // TODO: 处理物理移动 (Rigidbody)
        // if (_inputMove != Vector2.zero) { ... }
    }

    // Action: Move (Vector2)
    private void OnMove(InputAction.CallbackContext ctx)
    {
        // [持续性] 更新缓存
        _inputMove = ctx.ReadValue<Vector2>();
    }

    // Action: Jump (Button)
    private void OnJump(InputAction.CallbackContext ctx)
    {
        // [瞬发] 按下瞬间执行
        if (ctx.performed)
        {
            Debug.Log("Jump Performed");
            // TODO: 执行逻辑
        }
    }

    // Action: Fire (Button)
    private void OnFire(InputAction.CallbackContext ctx)
    {
        // [瞬发] 按下瞬间执行
        if (ctx.performed)
        {
            Debug.Log("Fire Performed");
            // TODO: 执行逻辑
        }
    }

    // Action: Dash (Button)
    private void OnDash(InputAction.CallbackContext ctx)
    {
        // [瞬发] 按下瞬间执行
        if (ctx.performed)
        {
            Debug.Log("Dash Performed");
            // TODO: 执行逻辑
        }
    }

}
