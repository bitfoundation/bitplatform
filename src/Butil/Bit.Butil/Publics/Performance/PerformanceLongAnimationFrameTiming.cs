namespace Bit.Butil;

/// <summary>
/// A frame the browser took more than 50 ms to produce, split into the work that caused it: the
/// scripts that ran, and the rendering that followed them.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceLongAnimationFrameTiming">https://developer.mozilla.org/en-US/docs/Web/API/PerformanceLongAnimationFrameTiming</see>
/// </summary>
/// <remarks>
/// The successor to <see cref="PerformanceLongTaskTiming"/>, and the reason to prefer it: a long
/// task says only that <i>something</i> took 200 ms, while a long animation frame names the
/// listener, the function and the file. Chromium-only at the time of writing; observing
/// <c>"long-animation-frame"</c> where it is unsupported yields nothing rather than failing.
/// </remarks>
public class PerformanceLongAnimationFrameTiming : PerformanceEntry
{
    /// <summary>When rendering started, after the scripts had run.</summary>
    public double RenderStart { get; set; }

    /// <summary>When style and layout started, inside rendering.</summary>
    public double StyleAndLayoutStart { get; set; }

    /// <summary>When the first UI event - a click, a keypress - was queued during this frame.</summary>
    public double FirstUIEventTimestamp { get; set; }

    /// <summary>Milliseconds of this frame that count as blocking the main thread.</summary>
    public double BlockingDuration { get; set; }

    /// <summary>The scripts that ran during the frame, each named down to the function.</summary>
    public PerformanceScriptTiming[] Scripts { get; set; } = [];
}
