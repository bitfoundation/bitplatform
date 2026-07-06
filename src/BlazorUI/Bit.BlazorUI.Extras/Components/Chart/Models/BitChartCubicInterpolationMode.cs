namespace Bit.BlazorUI;

/// <summary>Cubic interpolation mode for line smoothing, mirroring Chart.js.</summary>
public enum BitChartCubicInterpolationMode
{
    /// <summary>Cardinal spline using the dataset tension.</summary>
    Default,
    /// <summary>Monotone cubic interpolation that never overshoots the data (good for monotonic series).</summary>
    Monotone
}
