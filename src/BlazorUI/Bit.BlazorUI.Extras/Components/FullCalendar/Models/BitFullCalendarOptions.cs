namespace Bit.BlazorUI;

/// <summary>
/// Configuration options for the <see cref="BitFullCalendar"/> component.
/// These values are applied as initial defaults when the component mounts,
/// or whenever a new <see cref="BitFullCalendarOptions"/> instance is assigned.
/// </summary>
public class BitFullCalendarOptions
{
    /// <summary>Uses 24-hour time format instead of 12-hour (AM/PM).</summary>
    public bool Use24HourFormat { get; set; } = true;

    /// <summary>Badge display style in the month view.</summary>
    public BitFullCalendarBadgeVariant BadgeVariant { get; set; } = BitFullCalendarBadgeVariant.Colored;

    /// <summary>Hour (0–16) at which the day/week time grid begins. Values outside the range are clamped.</summary>
    public int StartOfDayHour
    {
        get => _startOfDayHour;
        set => _startOfDayHour = Math.Clamp(value, 0, 16);
    }
    private int _startOfDayHour = 8;

    /// <summary>How events are grouped in the agenda view.</summary>
    public BitFullCalendarAgendaGroupBy AgendaModeGroupBy { get; set; } = BitFullCalendarAgendaGroupBy.Date;

    /// <summary>How overlapping event cards are positioned in the day and week views.</summary>
    public BitFullCalendarEventLayout EventLayout { get; set; } = BitFullCalendarEventLayout.Overlap;
}
