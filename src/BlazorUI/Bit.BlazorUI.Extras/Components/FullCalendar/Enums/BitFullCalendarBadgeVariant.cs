namespace Bit.BlazorUI;

/// <summary>
/// How <see cref="BitFullCalendar"/> draws an event's color on the month badges and on the day, week and timeline
/// blocks (<see cref="BitFullCalendarSettings.BadgeVariant"/>).
/// </summary>
public enum BitFullCalendarBadgeVariant
{
    /// <summary>The event is filled with its color.</summary>
    Colored,

    /// <summary>The event is drawn on the calendar's neutral surface, with a dot of its color.</summary>
    Dot
}
