namespace Bit.BlazorUI;

public class BitFullCalendarEvent
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    /// <summary>
    /// Identifier of the color (matches a <see cref="BitFullCalendarColorOption.Id"/> from the
    /// calendar's configured palette). Defaults to <see cref="BitFullCalendarColorScheme.FallbackColorId"/>
    /// so that out-of-the-box rendering keeps working with the built-in palette.
    /// </summary>
    public string Color { get; set; } = BitFullCalendarColorScheme.FallbackColorId;
    private List<BitFullCalendarAttendee> _attendees = [];
    /// <summary>
    /// Attendees of the event. Never <c>null</c>: assigning <c>null</c> coalesces to an empty list
    /// so downstream code can safely iterate without null checks.
    /// </summary>
    public List<BitFullCalendarAttendee> Attendees
    {
        get => _attendees;
        set => _attendees = value ?? [];
    }

    /// <summary>
    /// Optional resource identifier linking this event to a <see cref="BitFullCalendarResource"/>
    /// (for example a meeting room name or a machine id). Used by the resource timeline view to
    /// place the event on the matching resource row. <c>null</c> or empty means the event is unassigned.
    /// Whitespace-only values are normalized to <c>null</c> so a blank id can never map to a resource
    /// row, mirroring how <see cref="BitFullCalendarResource.Id"/> rejects blank identifiers.
    /// </summary>
    public string? Resource
    {
        get => _resource;
        set => _resource = string.IsNullOrWhiteSpace(value) ? null : value;
    }
    private string? _resource;

    /// <summary>
    /// Makes the event a recurring series. The calendar then renders the occurrences the rule produces
    /// instead of the event itself, the first one at <see cref="StartDate"/>, each lasting
    /// <see cref="Duration"/>. <c>null</c> for an event that does not repeat.
    /// <para>
    /// On an occurrence (see <see cref="IsOccurrence"/>) this is the rule of its series, shared with it.
    /// </para>
    /// </summary>
    public BitFullCalendarRecurrence? Recurrence { get; set; }

    /// <summary>
    /// Set on the occurrences the calendar generates for a recurring event - the ones its views, templates,
    /// and <c>OnEventClick</c> receive - to the <see cref="Id"/> of that series. <c>null</c> on every other event.
    /// </summary>
    public string? RecurringEventId { get; set; }

    /// <summary>
    /// Set together with <see cref="RecurringEventId"/> to the start the occurrence has in its series, which
    /// is what identifies it there (see <see cref="BitFullCalendarRecurrence.ExceptionDates"/>).
    /// </summary>
    public DateTime? OccurrenceDate { get; set; }

    /// <summary>True for the event defining a recurring series: it has a <see cref="Recurrence"/> and is not an occurrence.</summary>
    public bool IsRecurring => Recurrence is not null && RecurringEventId is null;

    /// <summary>True for an occurrence the calendar generated from a recurring series.</summary>
    public bool IsOccurrence => RecurringEventId is not null;

    public bool IsSingleDay => StartDate.Date == BitFullCalendarHelpers.GetInclusiveEndDate(this);
    public bool IsMultiDay => !IsSingleDay;
    public TimeSpan Duration => EndDate - StartDate;

    public object? Data { get; set; }
}

