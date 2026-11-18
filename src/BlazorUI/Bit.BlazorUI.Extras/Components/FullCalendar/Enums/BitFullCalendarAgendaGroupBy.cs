namespace Bit.BlazorUI;

/// <summary>
/// How the agenda view of <see cref="BitFullCalendar"/> groups its events
/// (<see cref="BitFullCalendarSettings.AgendaModeGroupBy"/>).
/// </summary>
public enum BitFullCalendarAgendaGroupBy
{
    /// <summary>One group per day an event starts on, in date order.</summary>
    Date,

    /// <summary>One group per event color, in the order of the calendar's <c>EventColorOptions</c> and titled with
    /// each color's label.</summary>
    Color
}
