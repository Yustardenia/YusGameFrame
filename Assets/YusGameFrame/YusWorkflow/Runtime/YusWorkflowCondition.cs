using System;

[Serializable]
public abstract class YusWorkflowCondition
{
    public virtual string DisplayName => GetType().Name;

    protected YusWorkflowContext Context { get; private set; }

    public virtual bool Evaluate(YusWorkflowContext context)
    {
        Context = context;
        return Evaluate();
    }

    protected virtual bool Evaluate() => false;
}
