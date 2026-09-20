namespace Bit.BlazorUI;

/// <summary>
/// The edge of the viewport the off-canvas drawer of a <see cref="BitNavPanel{TItem}"/> comes from.
/// </summary>
/// <remarks>
/// Only the drawer of a small screen is docked to an edge: on a wide screen the panel is a column in the
/// normal flow of the page, where its place is decided by the layout that holds it.
/// </remarks>
public enum BitNavPanelPosition
{
    /// <summary>
    /// The drawer comes from the starting edge of the text direction: the left in a left-to-right layout,
    /// the right in a right-to-left one.
    /// </summary>
    Start,

    /// <summary>
    /// The drawer comes from the ending edge of the text direction: the right in a left-to-right layout,
    /// the left in a right-to-left one.
    /// </summary>
    End
}
