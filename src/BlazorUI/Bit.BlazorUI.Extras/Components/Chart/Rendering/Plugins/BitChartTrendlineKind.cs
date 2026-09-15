namespace Bit.BlazorUI;

/// <summary>The curve a <see cref="BitChartTrendline"/> fits through its dataset.</summary>
public enum BitChartTrendlineKind
{
    /// <summary>Least-squares straight line - the classic "is it going up or down" answer.</summary>
    Linear,

    /// <summary>Trailing simple moving average over <see cref="BitChartTrendline.Period"/> points, which
    /// smooths a noisy series without straightening it.</summary>
    MovingAverage,

    /// <summary>A flat line at the mean of the series, for reading each point against the average.</summary>
    Average
}
