namespace Bit.Bmotion;

/// <summary>Data returned by the <see cref="BmotionScrollTracker"/> on each scroll event.</summary>
public class BmotionScrollInfo
{
    /// <summary>Horizontal scroll offset in pixels.</summary>
    public double ScrollX { get; init; }

    /// <summary>Vertical scroll offset in pixels.</summary>
    public double ScrollY { get; init; }

    /// <summary>Horizontal scroll progress 0–1.</summary>
    public double ProgressX { get; init; }

    /// <summary>Vertical scroll progress 0–1.</summary>
    public double ProgressY { get; init; }

    public double ScrollWidth { get; init; }
    public double ScrollHeight { get; init; }
    public double ClientWidth { get; init; }
    public double ClientHeight { get; init; }
}
