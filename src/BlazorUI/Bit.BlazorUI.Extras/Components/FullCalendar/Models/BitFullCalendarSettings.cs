namespace Bit.BlazorUI;

/// <summary>
/// Configuration settings for the <see cref="BitFullCalendar"/> component.
/// These values are applied as initial defaults when the component mounts, and afterwards whenever
/// the consumer changes one of them (either by assigning a new <see cref="BitFullCalendarSettings"/>
/// instance or by mutating this one in place).
/// <para>
/// Preferences the user changes from the built-in settings panel are written back onto this
/// instance, so a parent re-render never reverts them.
/// </para>
/// </summary>
public class BitFullCalendarSettings
{
    /// <summary>Uses 24-hour time format instead of 12-hour (AM/PM).</summary>
    public bool Use24HourFormat { get; set; } = true;

    /// <summary>Badge display style in the month view.</summary>
    public BitFullCalendarBadgeVariant BadgeVariant { get; set; } = BitFullCalendarBadgeVariant.Colored;

    /// <summary>
    /// Hour the day/week time grid scrolls to on first render. Clamped into the window described by
    /// <see cref="VisibleStartHour"/> / <see cref="VisibleEndHour"/> before it is applied.
    /// </summary>
    public int StartOfDayHour
    {
        get => _startOfDayHour;
        set => _startOfDayHour = Math.Clamp(value, 0, 23);
    }
    private int _startOfDayHour = 8;

    /// <summary>
    /// First hour rendered by the day, week, and timeline day/week time grids (0-23).
    /// Equivalent to <c>slotMinTime</c> in other calendar libraries. Defaults to <c>0</c> (midnight).
    /// </summary>
    public int VisibleStartHour
    {
        get => _visibleStartHour;
        set => _visibleStartHour = Math.Clamp(value, 0, 23);
    }
    private int _visibleStartHour;

    /// <summary>
    /// Exclusive last hour rendered by the day, week, and timeline day/week time grids (1-24).
    /// Equivalent to <c>slotMaxTime</c>. Defaults to <c>24</c> (the full day). A value at or below
    /// <see cref="VisibleStartHour"/> is corrected to one hour past it when the grid is built.
    /// </summary>
    public int VisibleEndHour
    {
        get => _visibleEndHour;
        set => _visibleEndHour = Math.Clamp(value, 1, 24);
    }
    private int _visibleEndHour = 24;

    /// <summary>
    /// Length in minutes of one selectable slot inside an hour of the day/week time grids, and the
    /// granularity drag-and-drop and resizing snap to. Equivalent to <c>slotDuration</c>.
    /// Only divisors of 60 are accepted (5, 6, 10, 12, 15, 20, 30, 60); any other value is rounded
    /// to the nearest accepted one. Defaults to <c>30</c>.
    /// </summary>
    public int SlotDurationMinutes
    {
        get => _slotDurationMinutes;
        set => _slotDurationMinutes = NormalizeSlotDuration(value);
    }
    private int _slotDurationMinutes = 30;

    /// <summary>How events are grouped in the agenda view.</summary>
    public BitFullCalendarAgendaGroupBy AgendaModeGroupBy { get; set; } = BitFullCalendarAgendaGroupBy.Date;

    /// <summary>How overlapping event cards are positioned in the day and week views.</summary>
    public BitFullCalendarEventLayout EventLayout { get; set; } = BitFullCalendarEventLayout.Overlap;

    /// <summary>Renders the mini calendar shown in the day view sidebar.</summary>
    public bool ShowDayViewCalendar { get; set; } = true;

    /// <summary>
    /// Days of the week removed from every date grid (week, month, year, and the timeline week
    /// layout). Use it to render a work week. A list that would hide every day is ignored.
    /// </summary>
    public IReadOnlyList<DayOfWeek>? HiddenDays { get; set; }

    /// <summary>
    /// Overrides the day the week starts on. When <c>null</c> (the default) the active culture's
    /// <c>DateTimeFormat.FirstDayOfWeek</c> is used.
    /// </summary>
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// Renders the ISO-8601 week number in the month grid and in the week view's time gutter.
    /// </summary>
    public bool ShowWeekNumbers { get; set; }

    /// <summary>Renders the "current time" indicator line on the day and week time grids.</summary>
    public bool ShowCurrentTimeIndicator { get; set; } = true;

    /// <summary>
    /// Number of event badges a month-grid cell renders before collapsing the rest behind the
    /// "+N more" affordance. Clamped to 1-10. Defaults to <c>3</c>.
    /// </summary>
    public int MaxEventsPerDayCell
    {
        get => _maxEventsPerDayCell;
        set => _maxEventsPerDayCell = Math.Clamp(value, 1, 10);
    }
    private int _maxEventsPerDayCell = 3;

    /// <summary>
    /// Makes the description field of the built-in add/edit dialog mandatory. Defaults to
    /// <c>false</c> - only the title is required, matching the other calendar libraries.
    /// </summary>
    public bool RequireEventDescription { get; set; }

    /// <summary>
    /// Allows two events on the same resource to occupy the same time range. When <c>false</c>, a
    /// drag, resize, or dialog save that would create an overlap is refused and the calendar shows
    /// the <see cref="BitFullCalendarTexts.EventOverlapMessage"/> notice instead.
    /// </summary>
    public bool AllowEventOverlap { get; set; } = true;

    /// <summary>
    /// Lets the user press and drag across the day or week time grid to select a range of slots,
    /// which opens the new event already spanning it. A plain click still creates a one-slot event.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool AllowRangeSelection { get; set; } = true;

    /// <summary>The accepted <see cref="SlotDurationMinutes"/> values, ascending.</summary>
    internal static readonly int[] SlotDurations = [5, 6, 10, 12, 15, 20, 30, 60];

    private static int NormalizeSlotDuration(int value)
    {
        if (value <= SlotDurations[0]) return SlotDurations[0];
        if (value >= SlotDurations[^1]) return SlotDurations[^1];

        var best = SlotDurations[0];
        var bestDistance = int.MaxValue;
        foreach (var candidate in SlotDurations)
        {
            var distance = Math.Abs(candidate - value);
            // Ties resolve to the larger candidate, so a value exactly between two accepted
            // durations lands on the coarser (cheaper to render) grid.
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }
        return best;
    }
}
