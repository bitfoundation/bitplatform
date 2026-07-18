namespace Bit.BlazorUI;

/// <summary>
/// Determines where a target item is positioned within the viewport when scrolling to it.
/// </summary>
public enum BitVirtualizeScrollAlignment
{
    /// <summary>
    /// Scroll the minimum amount required to bring the item fully into view.
    /// </summary>
    Auto,

    /// <summary>
    /// Align the item to the start (top/left) of the viewport.
    /// </summary>
    Start,

    /// <summary>
    /// Center the item within the viewport.
    /// </summary>
    Center,

    /// <summary>
    /// Align the item to the end (bottom/right) of the viewport.
    /// </summary>
    End
}
