using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class YusWorkflowNode
{
    [SerializeField] private string titleOverride;

    public virtual string DisplayName => GetType().Name;

    public virtual IEnumerable<string> GetOutputPortNames()
    {
        yield return "Next";
    }

    public virtual void OnEnter(YusWorkflowContext context) { }
    public virtual void OnUpdate(YusWorkflowContext context) { }
    public virtual void OnFixedUpdate(YusWorkflowContext context) { }
    public virtual void OnExit(YusWorkflowContext context) { }

    public string GetTitle()
    {
        return string.IsNullOrWhiteSpace(titleOverride) ? DisplayName : titleOverride;
    }

    public void SetTitle(string title)
    {
        titleOverride = title;
    }
}
