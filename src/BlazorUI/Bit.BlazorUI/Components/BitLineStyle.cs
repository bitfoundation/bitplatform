namespace Bit.BlazorUI;

/// <summary>
/// How a line a component draws is stroked - the rule of a separator, the connector of a timeline.
/// </summary>
/// <remarks>
/// The values are the CSS <c>border-style</c> keywords the library draws its lines with. Not every component
/// honours every one of them: <see cref="Double"/> needs a line at least three pixels thick to have room for
/// the gap between its two strokes, so a component that draws a hairline leaves it out. Each parameter names
/// the values it accepts, and falls back to its own default for the rest.
/// </remarks>
public enum BitLineStyle
{
    /// <summary>
    /// A continuous line, which is what a line is drawn as unless something says otherwise.
    /// </summary>
    Solid,

    /// <summary>
    /// A line of short dashes, which usually marks the stretch it spans as pending or estimated.
    /// </summary>
    Dashed,

    /// <summary>
    /// A line of dots, a lighter version of the dashed line.
    /// </summary>
    Dotted,

    /// <summary>
    /// Two parallel lines with a gap between them, which needs a line of at least three pixels to have room
    /// to be drawn.
    /// </summary>
    Double
}
