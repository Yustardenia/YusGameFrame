using Fungus;
using UnityEngine;

[CommandInfo("YusVN", "Say (Fast Forward Branch)", "Say text; if fast-forwarded, jump to a different block immediately.")]
public class SayFastForwardBranch : Say
{
    [Tooltip("If fast-forward triggers, jump to this block (optional).")]
    public BlockReference targetBlock;

    private Writer writer;
    private bool jumpTriggered;
    private int lastFastForwardFrame = -1;

    public override void OnEnter()
    {
        jumpTriggered = false;
        base.OnEnter();
        CacheWriter();
    }

    private void Update()
    {
        if (!IsExecuting) return;
        if (jumpTriggered) return;

        if (IsFastForwarding())
        {
            if (HasTarget())
            {
                TriggerJump();
            }
            else
            {
                TryFastForwardOnce();
            }

            return;
        }

        var isWriting = writer != null && writer.IsWriting;
        if (!isWriting) return;

#if ENABLE_INPUT_SYSTEM
        if ((UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame ?? false) ||
            (UnityEngine.InputSystem.Touchscreen.current?.primaryTouch?.press.wasPressedThisFrame ?? false))
#else
        if (Input.GetMouseButtonDown(0))
#endif
        {
            if (HasTarget()) TriggerJump();
            else TryFastForwardOnce();
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
#else
        if (Input.GetButtonDown("Cancel"))
#endif
        {
            if (HasTarget()) TriggerJump();
            else TryFastForwardOnce();
        }
    }

    private void CacheWriter()
    {
        var sayDialog = SayDialog.GetSayDialog();
        writer = sayDialog != null ? sayDialog.GetComponentInChildren<Writer>() : null;
    }

    private bool IsFastForwarding()
    {
        return FastForwardManager.Instance != null && FastForwardManager.Instance.IsFastForwarding;
    }

    private bool HasTarget()
    {
        return targetBlock.block != null && targetBlock.block.GetFlowchart() != null;
    }

    private void TryFastForwardOnce()
    {
        if (Time.frameCount == lastFastForwardFrame) return;
        if (writer == null) CacheWriter();
        if (writer == null || !writer.IsWriting) return;
        lastFastForwardFrame = Time.frameCount;
        writer.OnNextLineEvent();
    }

    private void TriggerJump()
    {
        if (!HasTarget()) return;

        jumpTriggered = true;
        StopParentBlock();
        targetBlock.block.GetFlowchart().ExecuteBlock(targetBlock.block);
    }

    public override string GetSummary()
    {
        var baseSummary = base.GetSummary();
        var target = targetBlock.block != null ? targetBlock.block.BlockName : "None";
        return $"{baseSummary} <color=orange>[FF ? {target}]</color>";
    }

    public override Color GetButtonColor()
    {
        return new Color32(255, 255, 180, 255);
    }
}

