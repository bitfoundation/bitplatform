namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Specifies how a data-point on the chart will be styled.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/configuration/elements.html#point-styles">here (Chart.js)</a>.</para>
/// <para>Some samples showcasing the different styles can be found <a href="https://www.chartjs.org/samples/latest/charts/line/point-styles.html">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartLegacyPointStyle : BitChartLegacyStringEnum
{
    /// <summary>
    /// The circle point style.
    /// </summary>
    public static BitChartLegacyPointStyle Circle => new BitChartLegacyPointStyle("circle");

    /// <summary>
    /// The cross point style.
    /// </summary>
    public static BitChartLegacyPointStyle Cross => new BitChartLegacyPointStyle("cross");

    /// <summary>
    /// The rotated (45°) cross point style.
    /// </summary>
    public static BitChartLegacyPointStyle CrossRot => new BitChartLegacyPointStyle("crossRot");

    /// <summary>
    /// The dash point style.
    /// <para>Unlike <see cref="Line"/>, this style only displayes a dash on the right side of the point.</para>
    /// </summary>
    public static BitChartLegacyPointStyle Dash => new BitChartLegacyPointStyle("dash");

    /// <summary>
    /// The line point style.
    /// <para>Unlike <see cref="Dash"/>, this style displayes a dash on both the right and the left side of the point.</para>
    /// </summary>
    public static BitChartLegacyPointStyle Line => new BitChartLegacyPointStyle("line");

    /// <summary>
    /// The rectangle point style.
    /// </summary>
    public static BitChartLegacyPointStyle Rect => new BitChartLegacyPointStyle("rect");

    /// <summary>
    /// The rounded rectangle point style.
    /// </summary>
    public static BitChartLegacyPointStyle RectRounded => new BitChartLegacyPointStyle("rectRounded");

    /// <summary>
    /// The rotated (45°) rectangle point style.
    /// </summary>
    public static BitChartLegacyPointStyle RectRot => new BitChartLegacyPointStyle("rectRot");

    /// <summary>
    /// The star point style.
    /// <para>Same as <see cref="Cross"/> and <see cref="CrossRot"/> overlapping.</para>
    /// </summary>
    public static BitChartLegacyPointStyle Star => new BitChartLegacyPointStyle("star");

    /// <summary>
    /// The triangle point style.
    /// </summary>
    public static BitChartLegacyPointStyle Triangle => new BitChartLegacyPointStyle("triangle");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartLegacyPointStyle"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartLegacyPointStyle(string stringValue) : base(stringValue) { }
}
