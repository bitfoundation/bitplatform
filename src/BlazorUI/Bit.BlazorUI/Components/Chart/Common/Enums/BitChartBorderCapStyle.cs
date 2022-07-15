namespace Bit.BlazorUI;

/// <summary>
/// Specifies the border cap style.
/// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/lineCap">here (MDN)</a>.</para>
/// </summary>
public sealed class BitChartBorderCapStyle : BitChartStringEnum
{
    /// <summary>
    /// The ends of lines are squared off at the endpoints.
    /// </summary>
    public static BitChartBorderCapStyle Butt => new BitChartBorderCapStyle("butt");

    /// <summary>
    /// The ends of lines are rounded.
    /// </summary>
    public static BitChartBorderCapStyle Round => new BitChartBorderCapStyle("round");

    /// <summary>
    /// The ends of lines are squared off by adding a box with an equal width and half the height of the line's thickness.
    /// </summary>
    public static BitChartBorderCapStyle Square => new BitChartBorderCapStyle("square");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartBorderCapStyle"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartBorderCapStyle(string stringValue) : base(stringValue) { }
}
