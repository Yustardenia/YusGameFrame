using UnityEngine;

public sealed class YusEventNameAttribute : PropertyAttribute
{
    public readonly bool AllowCustom;

    public YusEventNameAttribute(bool allowCustom = true)
    {
        AllowCustom = allowCustom;
    }
}

