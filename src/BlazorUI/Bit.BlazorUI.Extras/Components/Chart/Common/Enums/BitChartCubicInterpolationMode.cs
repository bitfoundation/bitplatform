namespace Bit.BlazorUI;

/// <summary>
/// Specifies the cubic interpolation mode.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/line.html#cubicinterpolationmode">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartCubicInterpolationMode : BitChartStringEnum
{
    /// <summary>
    /// The default cubic interpolation mode.
    /// The <see cref="BitChartCubicInterpolationMode.Default" /> algorithm uses a custom weighted cubic interpolation, which produces pleasant curves for all types of datasets.
    /// </summary>
    public static BitChartCubicInterpolationMode Default => new BitChartCubicInterpolationMode("default");

    /// <summary>
    /// The monotone cubic interpolation mode.
    /// The <see cref="BitChartCubicInterpolationMode.Monotone" /> algorithm is more suited to y = f(x) datasets:
    /// It preserves monotonicity (or piecewise monotonicity) of the dataset being interpolated, and ensures local extrema (if any) stay at input data points.
    /// </summary>
    public static BitChartCubicInterpolationMode Monotone => new BitChartCubicInterpolationMode("monotone");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartCubicInterpolationMode"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartCubicInterpolationMode(string stringValue) : base(stringValue) { }
}
