namespace Bit.BlazorUI;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/#common-configuration">here (Chart.js)</a>.
/// </summary>
public abstract class BitChartAxis
{
    /// <summary>
    /// Controls the axis global visibility (visible when <see cref="BitChartAxisDisplay.True"/>, hidden when <see cref="BitChartAxisDisplay.False"/>).
    /// When display: <see cref="BitChartAxisDisplay.Auto"/>, the axis is visible only if at least one associated dataset is visible.
    /// </summary>
    public BitChartAxisDisplay Display { get; set; }

    /// <summary>
    /// The weight used to sort the axis. Higher weights are further away from the chart area.
    /// </summary>
    public int? Weight { get; set; }

    // TODO: Maybe implement: https://www.chartjs.org/docs/latest/axes/#callbacks
    // public object Callbacks { get; set; }
}
