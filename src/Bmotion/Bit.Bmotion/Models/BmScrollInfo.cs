namespace Bit.Bmotion;

/// <summary>Data returned by the <see cref="BmotionScrollTracker"/> on each scroll event.</summary>
public class BmScrollInfo
{
    /// <summary>Horizontal scroll offset in pixels.</summary>
    public double ScrollX { get; init; }

    /// <summary>Vertical scroll offset in pixels.</summary>
    public double ScrollY { get; init; }

    /// <summary>Horizontal scroll progress 0–1.</summary>
    public double ProgressX { get; init; }

    /// <summary>Vertical scroll progress 0–1.</summary>
    public double ProgressY { get; init; }

    /// <summary>Total scrollable content width in pixels (including the part outside the viewport).</summary>
    public double ScrollWidth { get; init; }

    /// <summary>Total scrollable content height in pixels (including the part outside the viewport).</summary>
    public double ScrollHeight { get; init; }

    /// <summary>Visible viewport width in pixels (the currently displayed area).</summary>
    public double ClientWidth { get; init; }

    /// <summary>Visible viewport height in pixels (the currently displayed area).</summary>
    public double ClientHeight { get; init; }

    /// <summary>
    /// Progress (0–1) of the tracked target element between the configured scroll offsets,
    /// or <c>null</c> when no target is being tracked (see <see cref="BmScrollOptions"/>).
    /// </summary>
    public double? TargetProgress { get; init; }
}
