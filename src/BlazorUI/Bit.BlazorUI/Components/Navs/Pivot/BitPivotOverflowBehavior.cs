namespace Bit.BlazorUI;

public enum BitPivotOverflowBehavior
{
    /// <summary>
    /// Pivot links will overflow the container and may not be visible
    /// </summary>
    None,

    /// <summary>
    /// Display an overflow menu that contains the tabs that don't fit
    /// </summary>
    Menu,

    /// <summary>
    /// Display a scroll bar below of the tabs for moving between them
    /// </summary>
    Scroll,

    /// <summary>
    /// Display next and previous buttons to slide through the tabs that don't fit
    /// </summary>
    Slide
}
