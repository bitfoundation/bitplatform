namespace Bit.BlazorUI;

/// <summary>
/// Determines how a BitButtonGroup behaves when its items do not fit in the available space.
/// </summary>
public enum BitButtonGroupOverflow
{
    /// <summary>
    /// The items are kept on a single line and the overflowing part is clipped.
    /// </summary>
    Clip,

    /// <summary>
    /// The items wrap onto multiple lines.
    /// </summary>
    Wrap,

    /// <summary>
    /// The items are kept on a single line and the group becomes scrollable, without rendering a scrollbar.
    /// It can still be scrolled by swiping, by shift+wheel, and through the arrow keys.
    /// </summary>
    Scroll,

    /// <summary>
    /// The items are kept on a single line and the group becomes scrollable, with a visible scrollbar.
    /// The scrollbar is laid out inside the border of the group, which makes the group taller.
    /// </summary>
    Scrollbar
}
