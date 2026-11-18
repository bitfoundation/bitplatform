namespace Bit.Blow;

/// <summary>
/// Restricts the axis along which pan-on-scroll moves the viewport.
/// </summary>
public enum BlowPanOnScrollMode
{
    /// <summary>Scrolling pans on both axes.</summary>
    Free,

    /// <summary>Scrolling only pans horizontally.</summary>
    Horizontal,

    /// <summary>Scrolling only pans vertically.</summary>
    Vertical
}
