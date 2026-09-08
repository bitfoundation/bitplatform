namespace Bit.BlazorUI;

/// <summary>
/// The edges of the scrolling container a BitSticky is currently pinned to.
/// </summary>
/// <remarks>
/// These are the physical edges of the scrollport, the way the browser resolves them: a
/// <see cref="BitStickyPosition.Start"/> sticky reports <see cref="Left"/> in a left-to-right
/// container and <see cref="Right"/> in a right-to-left one.
/// <br />
/// More than one of them can be set at once, since an element pinned into a corner is held by the
/// two edges that meet there.
/// </remarks>
[Flags]
public enum BitStickyEdges
{
    /// <summary>
    /// The element is not pinned: it is travelling with the content of its scrolling container.
    /// </summary>
    None = 0,

    /// <summary>
    /// The element is pinned to the top edge of its scrolling container.
    /// </summary>
    Top = 1,

    /// <summary>
    /// The element is pinned to the bottom edge of its scrolling container.
    /// </summary>
    Bottom = 2,

    /// <summary>
    /// The element is pinned to the left edge of its scrolling container.
    /// </summary>
    Left = 4,

    /// <summary>
    /// The element is pinned to the right edge of its scrolling container.
    /// </summary>
    Right = 8,
}
