namespace Bit.BlazorUI;

/// <summary>
/// Top-level layout mode for the calendar surface.
/// </summary>
public enum BitFullCalendarMode
{
    /// <summary>
    /// Default mode: day, week, month, year, and agenda views with events placed on a date grid.
    /// </summary>
    Event,

    /// <summary>
    /// Resource-centric layout: resources occupy the vertical axis and time the horizontal axis.
    /// Available views are day (1 day, 24 hour columns), week (7 days, 168 hour columns), and
    /// month (one column per day in the month). Requires the <c>Resources</c> parameter on the
    /// calendar component.
    /// </summary>
    Timeline
}
