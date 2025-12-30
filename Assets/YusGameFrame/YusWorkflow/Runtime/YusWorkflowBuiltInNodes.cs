using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class YusWorkflowEmptyNode : YusWorkflowNode
{
    [TextArea] public string Note;
}

[Serializable]
public sealed class YusWorkflowLogNode : YusWorkflowNode
{
    public string Message = "Hello Workflow";

    protected override void OnEnter()
    {
        Debug.Log(Message);
    }
}

[Serializable]
public sealed class YusWorkflowWaitNode : YusWorkflowNode
{
    public float Seconds = 1f;

    protected override void OnEnter()
    {
        Context.Set($"wait:{Context.CurrentNodeGuid}:remaining", Seconds);
    }

    protected override void OnUpdate()
    {
        var key = $"wait:{Context.CurrentNodeGuid}:remaining";
        var remaining = Context.Get(key, Seconds);
        remaining -= Context.DeltaTime;
        Context.Set(key, remaining);
    }
}

[Serializable]
public sealed class YusWorkflowCond_CurrentWaitDone : YusWorkflowCondition
{
    protected override bool Evaluate()
    {
        var key = $"wait:{Context.CurrentNodeGuid}:remaining";
        return Context.Get(key, 0f) <= 0f;
    }
}

[Serializable]
public sealed class YusWorkflowCond_Always : YusWorkflowCondition
{
    protected override bool Evaluate() => true;
}

[Serializable]
public sealed class YusWorkflowBroadcastEventNode : YusWorkflowNode
{
    public string EventName;

    protected override void OnEnter()
    {
        if (string.IsNullOrWhiteSpace(EventName)) return;
        Context.Events.Publish(EventName);
    }
}

[Serializable]
public sealed class YusWorkflowWaitEventNode : YusWorkflowNode
{
    public string EventName;
    public bool ResetCountOnEnter = true;

    protected override void OnEnter()
    {
        if (string.IsNullOrWhiteSpace(EventName)) return;

        if (ResetCountOnEnter) Context.ResetEvent(EventName);

        var subKey = YusWorkflowContext.GetEventSubscriptionKey(Context.CurrentNodeGuid, EventName);
        var existing = Context.Get<IDisposable>(subKey, null);
        existing?.Dispose();

        var token = Context.Events.Subscribe(EventName, () => Context.MarkEventFired(EventName));
        Context.Set(subKey, token);
    }

    protected override void OnExit()
    {
        if (string.IsNullOrWhiteSpace(EventName)) return;
        var subKey = YusWorkflowContext.GetEventSubscriptionKey(Context.CurrentNodeGuid, EventName);
        var token = Context.Get<IDisposable>(subKey, null);
        token?.Dispose();
        Context.Set(subKey, null);
    }
}

[Serializable]
public sealed class YusWorkflowCond_EventReceived : YusWorkflowCondition
{
    public string EventName;

    protected override bool Evaluate()
    {
        if (string.IsNullOrWhiteSpace(EventName)) return false;
        return Context.GetEventCount(EventName) > 0;
    }
}
