using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class YusWorkflowNode
{
    [SerializeField] private string titleOverride;

    protected YusWorkflowContext Context { get; private set; }

    public virtual string DisplayName => GetType().Name;

    public virtual IEnumerable<string> GetOutputPortNames()
    {
        yield return "Next";
    }

    public virtual void OnEnter(YusWorkflowContext context)
    {
        Context = context;
        OnEnter();
    }

    public virtual void OnUpdate(YusWorkflowContext context)
    {
        Context = context;
        OnUpdate();
    }

    public virtual void OnFixedUpdate(YusWorkflowContext context)
    {
        Context = context;
        OnFixedUpdate();
    }

    public virtual void OnExit(YusWorkflowContext context)
    {
        Context = context;
        OnExit();
    }

    protected virtual void OnEnter() { }
    protected virtual void OnUpdate() { }
    protected virtual void OnFixedUpdate() { }
    protected virtual void OnExit() { }

    public string GetTitle()
    {
        return string.IsNullOrWhiteSpace(titleOverride) ? DisplayName : titleOverride;
    }

    public void SetTitle(string title)
    {
        titleOverride = title;
    }
}
