namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Specifies the cubic interpolation mode.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/line.html#cubicinterpolationmode">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartLegacyCubicInterpolationMode : BitChartLegacyStringEnum
{
    /// <summary>
    /// The default cubic interpolation mode.
    /// The <see cref="BitChartLegacyCubicInterpolationMode.Default" /> algorithm uses a custom weighted cubic interpolation, which produces pleasant curves for all types of datasets.
    /// </summary>
    public static BitChartLegacyCubicInterpolationMode Default => new BitChartLegacyCubicInterpolationMode("default");

    /// <summary>
    /// The monotone cubic interpolation mode.
    /// The <see cref="BitChartLegacyCubicInterpolationMode.Monotone" /> algorithm is more suited to y = f(x) datasets:
    /// It preserves monotonicity (or piecewise monotonicity) of the dataset being interpolated, and ensures local extrema (if any) stay at input data points.
    /// </summary>
    public static BitChartLegacyCubicInterpolationMode Monotone => new BitChartLegacyCubicInterpolationMode("monotone");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartLegacyCubicInterpolationMode"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartLegacyCubicInterpolationMode(string stringValue) : base(stringValue) { }
}
