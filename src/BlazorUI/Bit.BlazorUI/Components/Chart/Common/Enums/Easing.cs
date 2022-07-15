namespace Bit.BlazorUI;

/// <summary>
/// Represents an easing function supported by Chart.js. Details about the different
/// functions can be found here: <a href="https://easings.net"/>
/// <para>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/configuration/animations.html#easing">here (Chart.js)</a>.
/// </para>
/// </summary>
public sealed class Easing : StringEnum
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static Easing Linear => new Easing("linear");
    public static Easing EaseInQuad => new Easing("easeInQuad");
    public static Easing EaseOutQuad => new Easing("easeOutQuad");
    public static Easing EaseInOutQuad => new Easing("easeInOutQuad");
    public static Easing EaseInCubic => new Easing("easeInCubic");
    public static Easing EaseOutCubic => new Easing("easeOutCubic");
    public static Easing EaseInOutCubic => new Easing("easeInOutCubic");
    public static Easing EaseInQuart => new Easing("easeInQuart");
    public static Easing EaseOutQuart => new Easing("easeOutQuart");
    public static Easing EaseInOutQuart => new Easing("easeInOutQuart");
    public static Easing EaseInQuint => new Easing("easeInQuint");
    public static Easing EaseOutQuint => new Easing("easeOutQuint");
    public static Easing EaseInOutQuint => new Easing("easeInOutQuint");
    public static Easing EaseInSine => new Easing("easeInSine");
    public static Easing EaseOutSine => new Easing("easeOutSine");
    public static Easing EaseInOutSine => new Easing("easeInOutSine");
    public static Easing EaseInExpo => new Easing("easeInExpo");
    public static Easing EaseOutExpo => new Easing("easeOutExpo");
    public static Easing EaseInOutExpo => new Easing("easeInOutExpo");
    public static Easing EaseInCirc => new Easing("easeInCirc");
    public static Easing EaseOutCirc => new Easing("easeOutCirc");
    public static Easing EaseInOutCirc => new Easing("easeInOutCirc");
    public static Easing EaseInElastic => new Easing("easeInElastic");
    public static Easing EaseOutElastic => new Easing("easeOutElastic");
    public static Easing EaseInOutElastic => new Easing("easeInOutElastic");
    public static Easing EaseInBack => new Easing("easeInBack");
    public static Easing EaseOutBack => new Easing("easeOutBack");
    public static Easing EaseInOutBack => new Easing("easeInOutBack");
    public static Easing EaseInBounce => new Easing("easeInBounce");
    public static Easing EaseOutBounce => new Easing("easeOutBounce");
    public static Easing EaseInOutBounce => new Easing("easeInOutBounce");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private Easing(string stringRep) : base(stringRep) { }
}
