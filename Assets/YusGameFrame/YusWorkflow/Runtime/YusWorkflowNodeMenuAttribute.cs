using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class YusWorkflowNodeMenuAttribute : Attribute
{
    public string Path { get; }

    public YusWorkflowNodeMenuAttribute(string path)
    {
        Path = path;
    }
}

