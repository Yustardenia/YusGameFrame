using System;

[Serializable]
public abstract class YusWorkflowCondition
{
    public virtual string DisplayName => GetType().Name;

    public abstract bool Evaluate(YusWorkflowContext context);
}

