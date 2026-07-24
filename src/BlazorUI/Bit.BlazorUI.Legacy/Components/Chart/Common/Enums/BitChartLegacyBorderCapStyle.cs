namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Specifies the border cap style.
/// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/lineCap">here (MDN)</a>.</para>
/// </summary>
public sealed class BitChartLegacyBorderCapStyle : BitChartLegacyStringEnum
{
    /// <summary>
    /// The ends of lines are squared off at the endpoints.
    /// </summary>
    public static BitChartLegacyBorderCapStyle Butt => new BitChartLegacyBorderCapStyle("butt");

    /// <summary>
    /// The ends of lines are rounded.
    /// </summary>
    public static BitChartLegacyBorderCapStyle Round => new BitChartLegacyBorderCapStyle("round");

    /// <summary>
    /// The ends of lines are squared off by adding a box with an equal width and half the height of the line's thickness.
    /// </summary>
    public static BitChartLegacyBorderCapStyle Square => new BitChartLegacyBorderCapStyle("square");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartLegacyBorderCapStyle"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartLegacyBorderCapStyle(string stringValue) : base(stringValue) { }
}
