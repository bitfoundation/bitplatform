namespace Bit.BlazorUI;

/// <summary>
/// The style the line of the <see cref="BitSeparator"/> is drawn in.
/// </summary>
public enum BitSeparatorLineStyle
{
    /// <summary>
    /// A continuous line, which is the default.
    /// </summary>
    Solid,

    /// <summary>
    /// A line of short dashes.
    /// </summary>
    Dashed,

    /// <summary>
    /// A line of dots.
    /// </summary>
    Dotted,

    /// <summary>
    /// Two parallel lines with a gap between them, which needs a line of at least three pixels to
    /// have room to be drawn.
    /// </summary>
    Double
}
