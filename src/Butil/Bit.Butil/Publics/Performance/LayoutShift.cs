namespace Bit.Butil;

/// <summary>
/// Content moving after it was already visible - the raw material of Cumulative Layout Shift.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/LayoutShift">https://developer.mozilla.org/en-US/docs/Web/API/LayoutShift</see>
/// </summary>
/// <remarks>
/// A single entry is not CLS. The metric is the largest sum over a session window of shifts with
/// <see cref="HadRecentInput"/> false - a shift the user caused by clicking something does not count
/// against you. <see cref="Performance.GetWebVitals"/> does that windowing; this type is what it
/// does it over.
/// </remarks>
public class LayoutShift : PerformanceEntry
{
    /// <summary>The shift's impact score: the fraction of the viewport affected times the fraction it moved.</summary>
    public double Value { get; set; }

    /// <summary>True when the shift followed user input within 500 ms, which excludes it from CLS.</summary>
    public bool HadRecentInput { get; set; }

    /// <summary>When that input happened, in milliseconds since the time origin.</summary>
    public double LastInputTime { get; set; }

    /// <summary>The elements that moved, largest first. Empty on engines that do not report attribution.</summary>
    public LayoutShiftAttribution[] Sources { get; set; } = [];
}
