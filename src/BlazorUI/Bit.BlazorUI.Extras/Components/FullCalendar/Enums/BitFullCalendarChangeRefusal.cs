namespace Bit.BlazorUI;

/// <summary>
/// Why the calendar refused to commit a user-driven change (a drop, a resize, or a dialog save).
/// <see cref="None"/> means the change was applied.
/// </summary>
public enum BitFullCalendarChangeRefusal
{
    /// <summary>The change was applied (or there was nothing to apply).</summary>
    None,

    /// <summary>
    /// The calendar, the single event, or the edit permissions (<see cref="BitFullCalendarSettings.AllowEdit"/>,
    /// <see cref="BitFullCalendarSettings.AllowDrag"/>, <see cref="BitFullCalendarSettings.AllowResize"/>) do not allow
    /// the change, so nothing was changed.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// The resulting range would overlap another event on the same resource while
    /// <see cref="BitFullCalendarSettings.AllowEventOverlap"/> is <c>false</c>.
    /// </summary>
    Overlap,

    /// <summary>
    /// The resulting range falls outside the <c>MinDate</c>/<c>MaxDate</c> window the calendar is
    /// allowed to show.
    /// </summary>
    OutOfRange,

    /// <summary>
    /// The resulting range falls outside the business hours while
    /// <see cref="BitFullCalendarSettings.RestrictToBusinessHours"/> is <c>true</c>.
    /// </summary>
    OutsideBusinessHours,

    /// <summary>
    /// The resulting range would overlap an event marked <see cref="BitFullCalendarEvent.IsBlocking"/> on the same
    /// resource (or one blocking every resource).
    /// </summary>
    Blocked
}
