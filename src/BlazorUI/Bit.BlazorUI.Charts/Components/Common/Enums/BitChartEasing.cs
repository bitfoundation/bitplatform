namespace Bit.BlazorUI;

/// <summary>
/// Represents an easing function supported by Chart.js. Details about the different
/// functions can be found here: <a href="https://easings.net"/>
/// <para>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/configuration/animations.html#easing">here (Chart.js)</a>.
/// </para>
/// </summary>
public sealed class BitChartEasing : BitChartStringEnum
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static BitChartEasing Linear => new BitChartEasing("linear");
    public static BitChartEasing EaseInQuad => new BitChartEasing("easeInQuad");
    public static BitChartEasing EaseOutQuad => new BitChartEasing("easeOutQuad");
    public static BitChartEasing EaseInOutQuad => new BitChartEasing("easeInOutQuad");
    public static BitChartEasing EaseInCubic => new BitChartEasing("easeInCubic");
    public static BitChartEasing EaseOutCubic => new BitChartEasing("easeOutCubic");
    public static BitChartEasing EaseInOutCubic => new BitChartEasing("easeInOutCubic");
    public static BitChartEasing EaseInQuart => new BitChartEasing("easeInQuart");
    public static BitChartEasing EaseOutQuart => new BitChartEasing("easeOutQuart");
    public static BitChartEasing EaseInOutQuart => new BitChartEasing("easeInOutQuart");
    public static BitChartEasing EaseInQuint => new BitChartEasing("easeInQuint");
    public static BitChartEasing EaseOutQuint => new BitChartEasing("easeOutQuint");
    public static BitChartEasing EaseInOutQuint => new BitChartEasing("easeInOutQuint");
    public static BitChartEasing EaseInSine => new BitChartEasing("easeInSine");
    public static BitChartEasing EaseOutSine => new BitChartEasing("easeOutSine");
    public static BitChartEasing EaseInOutSine => new BitChartEasing("easeInOutSine");
    public static BitChartEasing EaseInExpo => new BitChartEasing("easeInExpo");
    public static BitChartEasing EaseOutExpo => new BitChartEasing("easeOutExpo");
    public static BitChartEasing EaseInOutExpo => new BitChartEasing("easeInOutExpo");
    public static BitChartEasing EaseInCirc => new BitChartEasing("easeInCirc");
    public static BitChartEasing EaseOutCirc => new BitChartEasing("easeOutCirc");
    public static BitChartEasing EaseInOutCirc => new BitChartEasing("easeInOutCirc");
    public static BitChartEasing EaseInElastic => new BitChartEasing("easeInElastic");
    public static BitChartEasing EaseOutElastic => new BitChartEasing("easeOutElastic");
    public static BitChartEasing EaseInOutElastic => new BitChartEasing("easeInOutElastic");
    public static BitChartEasing EaseInBack => new BitChartEasing("easeInBack");
    public static BitChartEasing EaseOutBack => new BitChartEasing("easeOutBack");
    public static BitChartEasing EaseInOutBack => new BitChartEasing("easeInOutBack");
    public static BitChartEasing EaseInBounce => new BitChartEasing("easeInBounce");
    public static BitChartEasing EaseOutBounce => new BitChartEasing("easeOutBounce");
    public static BitChartEasing EaseInOutBounce => new BitChartEasing("easeInOutBounce");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private BitChartEasing(string stringRep) : base(stringRep) { }
}
