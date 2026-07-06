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

    public bool IsSingleDay => StartDate.Date == BitFullCalendarHelpers.GetInclusiveEndDate(this);
    public bool IsMultiDay => !IsSingleDay;
    public TimeSpan Duration => EndDate - StartDate;

    public object? Data { get; set; }
}

