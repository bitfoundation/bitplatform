namespace Bit.BlazorUI;

/// <summary>
/// The edge of the screen the panel slides in from.
/// </summary>
public enum BitPanelPosition
{
    /// <summary>
    /// The start edge: the left in left-to-right, the right in right-to-left.
    /// </summary>
    Start,

    /// <summary>
    /// The end edge: the right in left-to-right, the left in right-to-left.
    /// </summary>
    End,

    /// <summary>
    /// The top edge.
    /// </summary>
    Top,

    /// <summary>
    /// The bottom edge.
    /// </summary>
    Bottom,
}
