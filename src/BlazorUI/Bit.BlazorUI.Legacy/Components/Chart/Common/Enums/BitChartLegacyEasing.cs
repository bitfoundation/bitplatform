namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents an easing function supported by Chart.js. Details about the different
/// functions can be found here: <a href="https://easings.net"/>
/// <para>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/configuration/animations.html#easing">here (Chart.js)</a>.
/// </para>
/// </summary>
public sealed class BitChartLegacyEasing : BitChartLegacyStringEnum
{
    public static BitChartLegacyEasing Linear => new BitChartLegacyEasing("linear");
    public static BitChartLegacyEasing EaseInQuad => new BitChartLegacyEasing("easeInQuad");
    public static BitChartLegacyEasing EaseOutQuad => new BitChartLegacyEasing("easeOutQuad");
    public static BitChartLegacyEasing EaseInOutQuad => new BitChartLegacyEasing("easeInOutQuad");
    public static BitChartLegacyEasing EaseInCubic => new BitChartLegacyEasing("easeInCubic");
    public static BitChartLegacyEasing EaseOutCubic => new BitChartLegacyEasing("easeOutCubic");
    public static BitChartLegacyEasing EaseInOutCubic => new BitChartLegacyEasing("easeInOutCubic");
    public static BitChartLegacyEasing EaseInQuart => new BitChartLegacyEasing("easeInQuart");
    public static BitChartLegacyEasing EaseOutQuart => new BitChartLegacyEasing("easeOutQuart");
    public static BitChartLegacyEasing EaseInOutQuart => new BitChartLegacyEasing("easeInOutQuart");
    public static BitChartLegacyEasing EaseInQuint => new BitChartLegacyEasing("easeInQuint");
    public static BitChartLegacyEasing EaseOutQuint => new BitChartLegacyEasing("easeOutQuint");
    public static BitChartLegacyEasing EaseInOutQuint => new BitChartLegacyEasing("easeInOutQuint");
    public static BitChartLegacyEasing EaseInSine => new BitChartLegacyEasing("easeInSine");
    public static BitChartLegacyEasing EaseOutSine => new BitChartLegacyEasing("easeOutSine");
    public static BitChartLegacyEasing EaseInOutSine => new BitChartLegacyEasing("easeInOutSine");
    public static BitChartLegacyEasing EaseInExpo => new BitChartLegacyEasing("easeInExpo");
    public static BitChartLegacyEasing EaseOutExpo => new BitChartLegacyEasing("easeOutExpo");
    public static BitChartLegacyEasing EaseInOutExpo => new BitChartLegacyEasing("easeInOutExpo");
    public static BitChartLegacyEasing EaseInCirc => new BitChartLegacyEasing("easeInCirc");
    public static BitChartLegacyEasing EaseOutCirc => new BitChartLegacyEasing("easeOutCirc");
    public static BitChartLegacyEasing EaseInOutCirc => new BitChartLegacyEasing("easeInOutCirc");
    public static BitChartLegacyEasing EaseInElastic => new BitChartLegacyEasing("easeInElastic");
    public static BitChartLegacyEasing EaseOutElastic => new BitChartLegacyEasing("easeOutElastic");
    public static BitChartLegacyEasing EaseInOutElastic => new BitChartLegacyEasing("easeInOutElastic");
    public static BitChartLegacyEasing EaseInBack => new BitChartLegacyEasing("easeInBack");
    public static BitChartLegacyEasing EaseOutBack => new BitChartLegacyEasing("easeOutBack");
    public static BitChartLegacyEasing EaseInOutBack => new BitChartLegacyEasing("easeInOutBack");
    public static BitChartLegacyEasing EaseInBounce => new BitChartLegacyEasing("easeInBounce");
    public static BitChartLegacyEasing EaseOutBounce => new BitChartLegacyEasing("easeOutBounce");
    public static BitChartLegacyEasing EaseInOutBounce => new BitChartLegacyEasing("easeInOutBounce");

    private BitChartLegacyEasing(string stringRep) : base(stringRep) { }
}
