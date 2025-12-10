using System;
using UnityEngine;

/// <summary>
/// 标记一个字段，使其在退出 Play Mode 后保留运行时修改的值。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class KeepValueAttribute : PropertyAttribute
{
    // 这是一个标记属性，无需逻辑
}

// --- [SceneSelector] 属性 ---
[AttributeUsage(AttributeTargets.Field)]
public class SceneSelectorAttribute : PropertyAttribute
{
    // 这是一个绘制属性，不需要逻辑
}

//[Get]
[AttributeUsage(AttributeTargets.Field)]
public class GetAttribute : Attribute
{
    public bool IncludeChildren { get; private set; }
    public GetAttribute(bool fromChildren = false) => this.IncludeChildren = fromChildren;
}

//[Watch]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class WatchAttribute : Attribute
{
    public string Label;
    public WatchAttribute(string label = null) => Label = label;
}