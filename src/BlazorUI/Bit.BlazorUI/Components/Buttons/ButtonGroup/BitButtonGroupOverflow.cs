namespace Bit.BlazorUI;

/// <summary>
/// Determines how a BitButtonGroup behaves when its items do not fit in the available space.
/// </summary>
public enum BitButtonGroupOverflow
{
    /// <summary>
    /// The items are kept on a single line and the overflowing part is clipped. A detached group lets it spill
    /// out instead, since it clips nothing at all - which is what keeps the focus ring of its buttons whole.
    /// </summary>
    Clip,

    /// <summary>
    /// The items wrap onto multiple lines.
    /// </summary>
    Wrap,

    /// <summary>
    /// The items are kept on a single line and the group becomes scrollable along the axis it is laid out on -
    /// sideways, or down a vertical group - without rendering a scrollbar. It can still be scrolled by swiping,
    /// by shift+wheel, and through the arrow keys, which bring the button they focus into view.
    /// </summary>
    Scroll,

    /// <summary>
    /// The same, with a visible scrollbar. The scrollbar is laid out inside the border of the group, so the group
    /// grows by the room it takes on the edge it sits on.
    /// </summary>
    Scrollbar
}
