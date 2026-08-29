namespace Bit.BlazorUI;

/// <summary>
/// Where the direct children of a <see cref="BitScrollablePane"/> come to rest in it while it snaps.
/// </summary>
/// <remarks>
/// This is the CSS <c>scroll-snap-align</c> property, put on the direct children of the pane. A pane whose
/// items sit inside a layout container of their own - the flex row a horizontal strip is laid out with -
/// has that container as its only child, so give the items the property directly there instead.
/// </remarks>
public enum BitScrollSnapAlign
{
    /// <summary>
    /// The children carry no snap position of their own.
    /// </summary>
    None,

    /// <summary>
    /// Each child comes to rest at the start of the pane.
    /// </summary>
    Start,

    /// <summary>
    /// Each child comes to rest in the middle of the pane.
    /// </summary>
    Center,

    /// <summary>
    /// Each child comes to rest at the end of the pane.
    /// </summary>
    End
}
