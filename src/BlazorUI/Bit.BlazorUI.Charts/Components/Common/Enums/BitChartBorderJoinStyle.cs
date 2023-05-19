namespace Bit.BlazorUI;

/// <summary>
/// Specifies the border join style.
/// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/lineJoin">here (MDN)</a>.</para>
/// </summary>
public sealed class BitChartBorderJoinStyle : BitChartStringEnum
{
    /// <summary>
    /// Fills an additional triangular area between the common endpoint of connected segments, and the separate outside rectangular corners of each segment.
    /// </summary>
    public static BitChartBorderJoinStyle Bevel => new BitChartBorderJoinStyle("bevel");

    /// <summary>
    /// Rounds off the corners of a shape by filling an additional sector of disc centered at the common endpoint of connected segments. The radius for these rounded corners is equal to the line width.
    /// </summary>
    public static BitChartBorderJoinStyle Round => new BitChartBorderJoinStyle("round");

    /// <summary>
    /// Connected segments are joined by extending their outside edges to connect at a single point, with the effect of filling an additional lozenge-shaped area.
    /// </summary>
    public static BitChartBorderJoinStyle Miter => new BitChartBorderJoinStyle("miter");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartBorderJoinStyle"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartBorderJoinStyle(string stringValue) : base(stringValue) { }
}
