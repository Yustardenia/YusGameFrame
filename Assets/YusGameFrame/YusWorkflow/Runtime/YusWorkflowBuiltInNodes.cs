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

    public override void OnEnter(YusWorkflowContext context)
    {
        Debug.Log(Message);
    }
}

[Serializable]
public sealed class YusWorkflowWaitNode : YusWorkflowNode
{
    public float Seconds = 1f;

    public override void OnEnter(YusWorkflowContext context)
    {
        context.Set($"wait:{context.CurrentNodeGuid}:remaining", Seconds);
    }

    public override void OnUpdate(YusWorkflowContext context)
    {
        var key = $"wait:{context.CurrentNodeGuid}:remaining";
        var remaining = context.Get(key, Seconds);
        remaining -= context.DeltaTime;
        context.Set(key, remaining);
    }
}

[Serializable]
public sealed class YusWorkflowCond_CurrentWaitDone : YusWorkflowCondition
{
    public override bool Evaluate(YusWorkflowContext context)
    {
        var key = $"wait:{context.CurrentNodeGuid}:remaining";
        return context.Get(key, 0f) <= 0f;
    }
}

[Serializable]
public sealed class YusWorkflowCond_Always : YusWorkflowCondition
{
    public override bool Evaluate(YusWorkflowContext context) => true;
}

[Serializable]
public sealed class YusWorkflowBroadcastEventNode : YusWorkflowNode
{
    public string EventName;

    public override void OnEnter(YusWorkflowContext context)
    {
        if (string.IsNullOrWhiteSpace(EventName)) return;
        context.Events.Publish(EventName);
    }
}

[Serializable]
public sealed class YusWorkflowWaitEventNode : YusWorkflowNode
{
    public string EventName;
    public bool ResetCountOnEnter = true;

    public override void OnEnter(YusWorkflowContext context)
    {
        if (string.IsNullOrWhiteSpace(EventName)) return;

        if (ResetCountOnEnter) context.ResetEvent(EventName);

        var subKey = YusWorkflowContext.GetEventSubscriptionKey(context.CurrentNodeGuid, EventName);
        var existing = context.Get<IDisposable>(subKey, null);
        existing?.Dispose();

        var token = context.Events.Subscribe(EventName, () => context.MarkEventFired(EventName));
        context.Set(subKey, token);
    }

    public override void OnExit(YusWorkflowContext context)
    {
        if (string.IsNullOrWhiteSpace(EventName)) return;
        var subKey = YusWorkflowContext.GetEventSubscriptionKey(context.CurrentNodeGuid, EventName);
        var token = context.Get<IDisposable>(subKey, null);
        token?.Dispose();
        context.Set(subKey, null);
    }
}

[Serializable]
public sealed class YusWorkflowCond_EventReceived : YusWorkflowCondition
{
    public string EventName;

    public override bool Evaluate(YusWorkflowContext context)
    {
        if (string.IsNullOrWhiteSpace(EventName)) return false;
        return context.GetEventCount(EventName) > 0;
    }
}
