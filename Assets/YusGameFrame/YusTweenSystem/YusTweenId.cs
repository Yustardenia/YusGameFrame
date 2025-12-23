#if YUS_DOTWEEN
using UnityEngine;

public enum YusTweenChannel : uint
{
    Scale = 1,
    PopupScale = 2,
    HoverScale = 3,
    PressScale = 4,

    Move = 10,
    MoveLocal = 11,

    Rotate = 20,
    RotateLocal = 21,
    InfiniteRotate = 22,
    Wiggle = 23,
    Flip = 24,

    PunchPosition = 30,
    PunchScale = 31,
    PunchRotation = 32,

    ShakePosition = 40,
    ShakeRotation = 41,
    ShakeScale = 42,

    Fade = 50,
    Color = 51,
}

public static class YusTweenId
{
    public static object Resolve(object id, Object target, YusTweenChannel channel)
    {
        if (id != null) return id;
        if (target == null) return null;
        return For(target, channel);
    }

    public static object For(Object target, YusTweenChannel channel)
    {
        if (target == null) return null;
        return ForInstanceId(target.GetInstanceID(), channel);
    }

    public static object ForInstanceId(int instanceId, YusTweenChannel channel)
    {
        long packed = ((long)instanceId << 32) | (uint)channel;
        return packed;
    }
}
#endif

