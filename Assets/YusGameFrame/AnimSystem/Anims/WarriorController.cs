using UnityEngine;
using UnityEngine.InputSystem;

public partial class WarriorController
{
    public Rigidbody rb;
    public float moveSpeed = 5f;
    public Vector2 inputMove;

    partial void OnInit()
    {
        rb = GetComponent<Rigidbody>();

        var input = YusInputManager.Instance;
        if (input == null)
        {
            Debug.LogWarning("[WarriorController] YusInputManager.Instance is null");
            return;
        }

        this.YusRegisterInput(
            input.GetAction("Gameplay", "Move"),
            ctx => inputMove = ctx.ReadValue<Vector2>(),
            ctx => inputMove = ctx.ReadValue<Vector2>()
        );

        this.YusRegisterInput(
            input.GetAction("Gameplay", "Fire"),
            OnAttackInput
        );

        // 启动 FSM（旧版风格：State 类名直接使用 Animator State 名）
        fsm.Start<WarriorIdleState>();
    }

    private void OnAttackInput(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        if (!(fsm.CurrentState is WarriorAttackState))
            fsm.ChangeState<WarriorAttackState>();
    }
}
