namespace Bit.BlazorUI;

/// <summary>
/// The time span currently visible in the content area, plus the effective
/// horizontal scale. Raised from <c>OnViewChanged</c> at most once per frame
/// after scroll, zoom or layout. Times are the unpadded viewport edges (not
/// the 10% culling pad). In-process only; does not cross the JS boundary.
/// </summary>
public sealed class BitResourceTimelineView
{
    /// <summary>Time at the left edge of the content area.</summary>
    public DateTimeOffset Start { get; init; }

    /// <summary>Time at the right edge of the content area.</summary>
    public DateTimeOffset End { get; init; }

    /// <summary>Effective horizontal scale, in pixels per hour.</summary>
    public double PixelsPerHour { get; init; }
}
