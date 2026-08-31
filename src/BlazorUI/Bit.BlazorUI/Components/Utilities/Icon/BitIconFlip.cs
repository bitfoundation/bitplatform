namespace Bit.BlazorUI;

/// <summary>
/// The axes an icon can be mirrored on.
/// </summary>
public enum BitIconFlip
{
    /// <summary>
    /// Mirrored left to right.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Mirrored top to bottom.
    /// </summary>
    Vertical,

    /// <summary>
    /// Mirrored on both axes, which is the same as a half turn for an asymmetric glyph.
    /// </summary>
    Both
}
