namespace Bit.BlazorUI;

/// <summary>
/// Specifies how a data-point on the chart will be styled.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/configuration/elements.html#point-styles">here (Chart.js)</a>.</para>
/// <para>Some samples showcasing the different styles can be found <a href="https://www.chartjs.org/samples/latest/charts/line/point-styles.html">here (Chart.js)</a>.</para>
/// </summary>
public sealed class PointStyle : StringEnum
{
    /// <summary>
    /// The circle point style.
    /// </summary>
    public static PointStyle Circle => new PointStyle("circle");

    /// <summary>
    /// The cross point style.
    /// </summary>
    public static PointStyle Cross => new PointStyle("cross");

    /// <summary>
    /// The rotated (45°) cross point style.
    /// </summary>
    public static PointStyle CrossRot => new PointStyle("crossRot");

    /// <summary>
    /// The dash point style.
    /// <para>Unlike <see cref="Line"/>, this style only displayes a dash on the right side of the point.</para>
    /// </summary>
    public static PointStyle Dash => new PointStyle("dash");

    /// <summary>
    /// The line point style.
    /// <para>Unlike <see cref="Dash"/>, this style displayes a dash on both the right and the left side of the point.</para>
    /// </summary>
    public static PointStyle Line => new PointStyle("line");

    /// <summary>
    /// The rectangle point style.
    /// </summary>
    public static PointStyle Rect => new PointStyle("rect");

    /// <summary>
    /// The rounded rectangle point style.
    /// </summary>
    public static PointStyle RectRounded => new PointStyle("rectRounded");

    /// <summary>
    /// The rotated (45°) rectangle point style.
    /// </summary>
    public static PointStyle RectRot => new PointStyle("rectRot");

    /// <summary>
    /// The star point style.
    /// <para>Same as <see cref="Cross"/> and <see cref="CrossRot"/> overlapping.</para>
    /// </summary>
    public static PointStyle Star => new PointStyle("star");

    /// <summary>
    /// The triangle point style.
    /// </summary>
    public static PointStyle Triangle => new PointStyle("triangle");

    /// <summary>
    /// Creates a new instance of the <see cref="PointStyle"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private PointStyle(string stringValue) : base(stringValue) { }
}
