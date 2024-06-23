namespace Bit.BlazorUI;

/// <summary>
/// Specifies how a data-point on the chart will be styled.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/configuration/elements.html#point-styles">here (Chart.js)</a>.</para>
/// <para>Some samples showcasing the different styles can be found <a href="https://www.chartjs.org/samples/latest/charts/line/point-styles.html">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartPointStyle : BitChartStringEnum
{
    /// <summary>
    /// The circle point style.
    /// </summary>
    public static BitChartPointStyle Circle => new BitChartPointStyle("circle");

    /// <summary>
    /// The cross point style.
    /// </summary>
    public static BitChartPointStyle Cross => new BitChartPointStyle("cross");

    /// <summary>
    /// The rotated (45°) cross point style.
    /// </summary>
    public static BitChartPointStyle CrossRot => new BitChartPointStyle("crossRot");

    /// <summary>
    /// The dash point style.
    /// <para>Unlike <see cref="Line"/>, this style only displayes a dash on the right side of the point.</para>
    /// </summary>
    public static BitChartPointStyle Dash => new BitChartPointStyle("dash");

    /// <summary>
    /// The line point style.
    /// <para>Unlike <see cref="Dash"/>, this style displayes a dash on both the right and the left side of the point.</para>
    /// </summary>
    public static BitChartPointStyle Line => new BitChartPointStyle("line");

    /// <summary>
    /// The rectangle point style.
    /// </summary>
    public static BitChartPointStyle Rect => new BitChartPointStyle("rect");

    /// <summary>
    /// The rounded rectangle point style.
    /// </summary>
    public static BitChartPointStyle RectRounded => new BitChartPointStyle("rectRounded");

    /// <summary>
    /// The rotated (45°) rectangle point style.
    /// </summary>
    public static BitChartPointStyle RectRot => new BitChartPointStyle("rectRot");

    /// <summary>
    /// The star point style.
    /// <para>Same as <see cref="Cross"/> and <see cref="CrossRot"/> overlapping.</para>
    /// </summary>
    public static BitChartPointStyle Star => new BitChartPointStyle("star");

    /// <summary>
    /// The triangle point style.
    /// </summary>
    public static BitChartPointStyle Triangle => new BitChartPointStyle("triangle");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartPointStyle"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartPointStyle(string stringValue) : base(stringValue) { }
}
