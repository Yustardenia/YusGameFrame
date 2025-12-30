using System;
using UnityEngine;

[Serializable]
public sealed class YusWorkflowBlackboardEntry
{
    public enum ValueKind
    {
        Int,
        Float,
        Bool,
        String
    }

    public string Key;
    public ValueKind Kind = ValueKind.Int;

    public int IntValue;
    public float FloatValue;
    public bool BoolValue;
    public string StringValue;

    public bool TryApplyTo(YusWorkflowContext context)
    {
        if (context == null) return false;
        if (string.IsNullOrWhiteSpace(Key)) return false;

        switch (Kind)
        {
            case ValueKind.Int:
                context.Set(Key, IntValue);
                return true;
            case ValueKind.Float:
                context.Set(Key, FloatValue);
                return true;
            case ValueKind.Bool:
                context.Set(Key, BoolValue);
                return true;
            case ValueKind.String:
                context.Set(Key, StringValue);
                return true;
            default:
                Debug.LogWarning($"[YusWorkflow] Unknown blackboard value kind: {Kind} (Key={Key})");
                return false;
        }
    }
}

