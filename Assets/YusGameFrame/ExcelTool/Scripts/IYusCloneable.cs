namespace UnityEngine { } // 防止有些环境报错，可不写

/// <summary>
/// 支持深拷贝的接口
/// </summary>
public interface IYusCloneable<T>
{
    T Clone();
}