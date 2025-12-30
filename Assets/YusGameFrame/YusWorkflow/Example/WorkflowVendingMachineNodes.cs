using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VMNode_Init : YusWorkflowNode
{
    protected override void OnEnter()
    {
        // Reset to the initial blackboard values configured on the WorkflowAsset (via the editor window).
        Context.ResetBlackboardToInitial();
        Debug.Log("[VMWF] Init: press 1/2 to select item.");
    }

    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Item, 1);
            Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Price, 2);
            Debug.Log("[VMWF] Selected Cola, price=2");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Item, 2);
            Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Price, 1);
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

    protected override void OnEnter()
    {
        var price = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Price, 0);
        var paid = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Paid, 0);
        Debug.Log($"[VMWF] Coin: price={price}, paid={paid}. (C=+1, X=cancel)");
        Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Cancel, false);
    }

    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            var paid = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Paid, 0) + 1;
            Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Paid, paid);
            var price = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Price, 0);
            Debug.Log($"[VMWF] Insert coin: {paid}/{price}");
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Cancel, true);
            Debug.Log("[VMWF] Cancel requested.");
        }
    }
}

[Serializable]
public sealed class VMNode_Dispense : YusWorkflowNode
{
    public float Seconds = 1.2f;

    protected override void OnEnter()
    {
        Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_DispenseRemaining, Seconds);
        Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_DispenseDone, false);
        var item = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Item, 0);
        Debug.Log($"[VMWF] Dispense item={item}...");
    }

    protected override void OnUpdate()
    {
        var remaining = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_DispenseRemaining, Seconds);
        remaining -= Context.DeltaTime;
        Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_DispenseRemaining, remaining);
        if (remaining > 0f) return;

        Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_DispenseDone, true);
        Debug.Log("[VMWF] Dispense done.");
    }
}

[Serializable]
public sealed class VMNode_End : YusWorkflowNode
{
    protected override void OnEnter()
    {
        var paid = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Paid, 0);
        var price = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Price, 0);
        Debug.Log($"[VMWF] End. Change={Mathf.Max(0, paid - price)}. (R=restart)");
        Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Restart, false);
    }

    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Restart, true);
        }
    }

    protected override void OnExit()
    {
        Context.Set(VendingMachineWorkflowBlackboardKeys.Vm_Restart, false);
    }
}

[Serializable]
public sealed class VMCond_HasSelection : YusWorkflowCondition
{
    protected override bool Evaluate() => Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Item, 0) != 0;
}

[Serializable]
public sealed class VMCond_PaidEnough : YusWorkflowCondition
{
    protected override bool Evaluate()
    {
        var price = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Price, 0);
        var paid = Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Paid, 0);
        return price > 0 && paid >= price;
    }
}

[Serializable]
public sealed class VMCond_Cancel : YusWorkflowCondition
{
    protected override bool Evaluate() => Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Cancel, false);
}

[Serializable]
public sealed class VMCond_DispenseDone : YusWorkflowCondition
{
    protected override bool Evaluate() => Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_DispenseDone, false);
}

[Serializable]
public sealed class VMCond_Restart : YusWorkflowCondition
{
    protected override bool Evaluate() => Context.Get(VendingMachineWorkflowBlackboardKeys.Vm_Restart, false);
}
