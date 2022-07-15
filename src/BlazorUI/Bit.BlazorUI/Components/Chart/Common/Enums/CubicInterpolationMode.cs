namespace Bit.BlazorUI;

/// <summary>
/// Specifies the cubic interpolation mode.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/line.html#cubicinterpolationmode">here (Chart.js)</a>.</para>
/// </summary>
public sealed class CubicInterpolationMode : StringEnum
{
    /// <summary>
    /// The default cubic interpolation mode.
    /// The <see cref="CubicInterpolationMode.Default" /> algorithm uses a custom weighted cubic interpolation, which produces pleasant curves for all types of datasets.
    /// </summary>
    public static CubicInterpolationMode Default => new CubicInterpolationMode("default");

    /// <summary>
    /// The monotone cubic interpolation mode.
    /// The <see cref="CubicInterpolationMode.Monotone" /> algorithm is more suited to y = f(x) datasets:
    /// It preserves monotonicity (or piecewise monotonicity) of the dataset being interpolated, and ensures local extrema (if any) stay at input data points.
    /// </summary>
    public static CubicInterpolationMode Monotone => new CubicInterpolationMode("monotone");

    /// <summary>
    /// Creates a new instance of the <see cref="CubicInterpolationMode"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private CubicInterpolationMode(string stringValue) : base(stringValue) { }
}
