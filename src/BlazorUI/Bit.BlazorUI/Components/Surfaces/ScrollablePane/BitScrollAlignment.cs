namespace Bit.BlazorUI;

/// <summary>
/// Where inside a <see cref="BitScrollablePane"/> an element is left after it has been scrolled into view.
/// </summary>
public enum BitScrollAlignment
{
    /// <summary>
    /// The element is brought to the start of the pane: its top edge to the top of the pane, and its
    /// leading edge to the leading edge of the pane.
    /// </summary>
    Start,

    /// <summary>
    /// The element is centered in the pane along both axes.
    /// </summary>
    Center,

    /// <summary>
    /// The element is brought to the end of the pane: its bottom edge to the bottom of the pane, and its
    /// trailing edge to the trailing edge of the pane.
    /// </summary>
    End,

    /// <summary>
    /// The pane moves as little as it can: an element that is already fully in view is not moved to at all,
    /// and one that is not is brought to whichever edge it is nearest.
    /// </summary>
    Nearest
}
