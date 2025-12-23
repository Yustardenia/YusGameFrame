using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VMNode_Init : YusWorkflowNode
{
    public override void OnEnter(YusWorkflowContext context)
    {
        context.Set("vm:item", 0);
        context.Set("vm:price", 0);
        context.Set("vm:paid", 0);
        context.Set("vm:cancel", false);
        context.Set("vm:dispenseDone", false);
        Debug.Log("[VMWF] Init: press 1/2 to select item.");
    }

    public override void OnUpdate(YusWorkflowContext context)
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            context.Set("vm:item", 1);
            context.Set("vm:price", 2);
            Debug.Log("[VMWF] Selected Cola, price=2");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            context.Set("vm:item", 2);
            context.Set("vm:price", 1);
            Debug.Log("[VMWF] Selected Water, price=1");
        }
    }
}

[Serializable]
public sealed class VMNode_Coin : YusWorkflowNode
{
    public override IEnumerable<string> GetOutputPortNames()
    {
        yield return "Paid";
        yield return "Cancel";
    }

    public override void OnEnter(YusWorkflowContext context)
    {
        var price = context.Get("vm:price", 0);
        var paid = context.Get("vm:paid", 0);
        Debug.Log($"[VMWF] Coin: price={price}, paid={paid}. (C=+1, X=cancel)");
        context.Set("vm:cancel", false);
    }

    public override void OnUpdate(YusWorkflowContext context)
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            var paid = context.Get("vm:paid", 0) + 1;
            context.Set("vm:paid", paid);
            var price = context.Get("vm:price", 0);
            Debug.Log($"[VMWF] Insert coin: {paid}/{price}");
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            context.Set("vm:cancel", true);
            Debug.Log("[VMWF] Cancel requested.");
        }
    }
}

[Serializable]
public sealed class VMNode_Dispense : YusWorkflowNode
{
    public float Seconds = 1.2f;

    public override void OnEnter(YusWorkflowContext context)
    {
        context.Set("vm:dispenseRemaining", Seconds);
        context.Set("vm:dispenseDone", false);
        var item = context.Get("vm:item", 0);
        Debug.Log($"[VMWF] Dispense item={item}...");
    }

    public override void OnUpdate(YusWorkflowContext context)
    {
        var remaining = context.Get("vm:dispenseRemaining", Seconds);
        remaining -= context.DeltaTime;
        context.Set("vm:dispenseRemaining", remaining);
        if (remaining > 0f) return;

        context.Set("vm:dispenseDone", true);
        Debug.Log("[VMWF] Dispense done.");
    }
}

[Serializable]
public sealed class VMNode_End : YusWorkflowNode
{
    public override void OnEnter(YusWorkflowContext context)
    {
        var paid = context.Get("vm:paid", 0);
        var price = context.Get("vm:price", 0);
        Debug.Log($"[VMWF] End. Change={Mathf.Max(0, paid - price)}. (R=restart)");
        context.Set("vm:restart", false);
    }

    public override void OnUpdate(YusWorkflowContext context)
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            context.Set("vm:restart", true);
        }
    }

    public override void OnExit(YusWorkflowContext context)
    {
        context.Set("vm:restart", false);
    }
}

[Serializable]
public sealed class VMCond_HasSelection : YusWorkflowCondition
{
    public override bool Evaluate(YusWorkflowContext context) => context.Get("vm:item", 0) != 0;
}

[Serializable]
public sealed class VMCond_PaidEnough : YusWorkflowCondition
{
    public override bool Evaluate(YusWorkflowContext context)
    {
        var price = context.Get("vm:price", 0);
        var paid = context.Get("vm:paid", 0);
        return price > 0 && paid >= price;
    }
}

[Serializable]
public sealed class VMCond_Cancel : YusWorkflowCondition
{
    public override bool Evaluate(YusWorkflowContext context) => context.Get("vm:cancel", false);
}

[Serializable]
public sealed class VMCond_DispenseDone : YusWorkflowCondition
{
    public override bool Evaluate(YusWorkflowContext context) => context.Get("vm:dispenseDone", false);
}

[Serializable]
public sealed class VMCond_Restart : YusWorkflowCondition
{
    public override bool Evaluate(YusWorkflowContext context) => context.Get("vm:restart", false);
}

