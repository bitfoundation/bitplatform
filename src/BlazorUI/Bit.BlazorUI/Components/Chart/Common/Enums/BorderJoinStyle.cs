namespace Bit.BlazorUI;

/// <summary>
/// Specifies the border join style.
/// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/lineJoin">here (MDN)</a>.</para>
/// </summary>
public sealed class BorderJoinStyle : StringEnum
{
    /// <summary>
    /// Fills an additional triangular area between the common endpoint of connected segments, and the separate outside rectangular corners of each segment.
    /// </summary>
    public static BorderJoinStyle Bevel => new BorderJoinStyle("bevel");

    /// <summary>
    /// Rounds off the corners of a shape by filling an additional sector of disc centered at the common endpoint of connected segments. The radius for these rounded corners is equal to the line width.
    /// </summary>
    public static BorderJoinStyle Round => new BorderJoinStyle("round");

    /// <summary>
    /// Connected segments are joined by extending their outside edges to connect at a single point, with the effect of filling an additional lozenge-shaped area.
    /// </summary>
    public static BorderJoinStyle Miter => new BorderJoinStyle("miter");

    /// <summary>
    /// Creates a new instance of the <see cref="BorderJoinStyle"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BorderJoinStyle(string stringValue) : base(stringValue) { }
}
