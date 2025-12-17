#if YUS_DOTWEEN
using DG.Tweening;

/// <summary>
/// 常用缓动预设：把“难记的枚举名”翻译成“人话按钮”。
/// 你可以把它当成美术/策划沟通用的词汇表：说“弹一下”就是 PopOut，说“快速收回”就是 FastPullBack。
/// </summary>
public static class YusEase
{
    /// <summary>
    /// 平均速：不拐弯，不加速，像电梯匀速上下。
    /// </summary>
    public const Ease Average = Ease.Linear;

    /// <summary>
    /// 慢到快：先犹豫一下再跑起来，适合开始阶段比较“稳”的动作。
    /// </summary>
    public const Ease SlowToFast = Ease.InQuad;

    /// <summary>
    /// 快到慢：先冲出去再刹车，适合自然落点、结束阶段的“收住”。
    /// </summary>
    public const Ease FastToSlow = Ease.OutQuad;

    /// <summary>
    /// 利落停下：比 FastToSlow 更“利落”，适合大部分 UI/移动的默认选择。
    /// </summary>
    public const Ease CleanStop = Ease.OutCubic;

    /// <summary>
    /// 快速收回：结束时像被吸回去一样干脆，适合关闭/消失。
    /// </summary>
    public const Ease FastPullBack = Ease.InCubic;

    /// <summary>
    /// 弹一下：带一点夸张的回弹，适合弹窗出现、奖励提示、按钮强调。
    /// </summary>
    public const Ease PopOut = Ease.OutBack;

    /// <summary>
    /// 向内弹回：像把弹簧往里按，适合“缩回/消失”的收尾。
    /// </summary>
    public const Ease PopIn = Ease.InBack;

    /// <summary>
    /// 火箭起步：前期非常慢、后期非常快，适合“突然冲刺”的感觉。
    /// </summary>
    public const Ease RocketStart = Ease.InExpo;

    /// <summary>
    /// 火箭刹车：前期很快、后期很慢，适合“冲出去然后稳稳停住”。
    /// </summary>
    public const Ease RocketBrake = Ease.OutExpo;

    // --- Backward-compatible aliases (枚举名好记，但项目里可能已经用上了，先留着不拆家) ---

    public const Ease Linear = Average;
    public const Ease InQuad = SlowToFast;
    public const Ease OutQuad = FastToSlow;
    public const Ease OutCubic = CleanStop;
    public const Ease InCubic = FastPullBack;
    public const Ease OutBack = PopOut;
    public const Ease InBack = PopIn;
    public const Ease InExpo = RocketStart;
    public const Ease OutExpo = RocketBrake;
}

#endif
