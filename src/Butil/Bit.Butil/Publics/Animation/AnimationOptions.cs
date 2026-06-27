namespace Bit.Butil;

/// <summary>
/// Subset of <see href="https://developer.mozilla.org/en-US/docs/Web/API/KeyframeEffect/KeyframeEffect#options">KeyframeEffectOptions</see>
/// that we forward across interop.
/// </summary>
public class AnimationOptions
{
    /// <summary>Total duration in milliseconds.</summary>
    public double Duration { get; set; } = 1000;

    /// <summary>Delay before playback starts, in milliseconds.</summary>
    public double Delay { get; set; }

    /// <summary>Delay after playback completes, in milliseconds.</summary>
    public double EndDelay { get; set; }

    /// <summary>Number of iterations. Use <see cref="double.PositiveInfinity"/> to loop forever.</summary>
    public double Iterations { get; set; } = 1;

    /// <summary>Easing - e.g. <c>"linear"</c>, <c>"ease-in"</c>, <c>"cubic-bezier(0,0,0.2,1)"</c>.</summary>
    public string Easing { get; set; } = "linear";

    /// <summary>One of <c>"normal"</c>, <c>"reverse"</c>, <c>"alternate"</c>, <c>"alternate-reverse"</c>.</summary>
    public string Direction { get; set; } = "normal";

    /// <summary>One of <c>"none"</c>, <c>"forwards"</c>, <c>"backwards"</c>, <c>"both"</c>, <c>"auto"</c>.</summary>
    public string Fill { get; set; } = "none";

    /// <summary>Composite operation: <c>"replace"</c>, <c>"add"</c>, <c>"accumulate"</c>.</summary>
    public string Composite { get; set; } = "replace";
}
