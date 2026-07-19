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
    /// The items are kept on a single line and the group becomes scrollable.
    /// </summary>
    Scroll
}
