using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Auto-generated style input controller example.
/// For dialogue/cutscene, consider `using (YusInputManager.Instance.AcquireUI()) { ... }`.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Input Cache")]
    [SerializeField] private Vector2 _inputMove;

    private void Start()
    {
        var input = YusInputManager.Instance;
        if (input == null)
        {
            Debug.LogWarning("[PlayerController] YusInputManager.Instance is null");
            return;
        }

        this.YusRegisterInput(input.GetAction("Gameplay", "Move"), OnMove);
        this.YusRegisterInput(input.GetAction("Gameplay", "Jump"), OnJump);
        this.YusRegisterInput(input.GetAction("Gameplay", "Fire"), OnFire);
        this.YusRegisterInput(input.GetAction("Gameplay", "Dash"), OnDash);
    }

    private void Update()
    {
        // TODO: non-physics logic
    }

    private void FixedUpdate()
    {
        // TODO: physics movement
        // if (_inputMove != Vector2.zero) { ... }
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        _inputMove = ctx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            Debug.Log("Jump Performed");
        }
    }

    private void OnFire(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            Debug.Log("Fire Performed");
        }
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            Debug.Log("Dash Performed");
        }
    }
}

