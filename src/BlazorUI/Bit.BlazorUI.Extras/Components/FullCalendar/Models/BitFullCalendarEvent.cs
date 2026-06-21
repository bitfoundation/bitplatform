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
    public List<BitFullCalendarAttendee> Attendees { get; set; } = [];

    /// <summary>
    /// Optional resource identifier linking this event to a <see cref="BitFullCalendarResource"/>
    /// (for example a meeting room name or a machine id). Used by the resource timeline view to
    /// place the event on the matching resource row. <c>null</c> or empty means the event is unassigned.
    /// </summary>
    public string? Resource { get; set; }

    public bool IsSingleDay => StartDate.Date == (EndDate > StartDate ? EndDate.AddTicks(-1) : EndDate).Date;
    public bool IsMultiDay => !IsSingleDay;
    public TimeSpan Duration => EndDate - StartDate;

    public object? Data { get; set; }
}

