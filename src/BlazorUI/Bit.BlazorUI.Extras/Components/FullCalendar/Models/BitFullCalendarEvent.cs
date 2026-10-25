namespace Bit.BlazorUI;

/// <summary>
/// A single calendar event rendered across the day, week, month, year, agenda, and timeline views.
/// </summary>
public class BitFullCalendarEvent
{
    /// <summary>
    /// Unique identifier of the event. The calendar matches the events it holds by this value, so it should be
    /// stable across re-renders.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Event title shown on the event card, badge, and dialogs; it is also the event's accessible name.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Event description shown in the details and add/edit dialogs.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Start date and time of the event.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date and time of the event (exclusive).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Identifier of the color (matches a <see cref="BitFullCalendarColorOption.Id"/> from the
    /// calendar's configured palette). Defaults to <see cref="BitFullCalendarColorScheme.FallbackColorId"/>
    /// so that out-of-the-box rendering keeps working with the built-in palette.
    /// <para>
    /// A value no option matches can be a CSS color of its own - a hex (<c>"#e91e63"</c>) or a color function
    /// (<c>"rgb(...)"</c>, <c>"hsl(...)"</c>, <c>"oklch(...)"</c>, <c>"var(...)"</c>) - for colors that come with the
    /// data rather than from a palette. Anything else is drawn in the default event color
    /// (<c>--bit-FullCalendar-event-color</c>).
    /// </para>
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
    /// Marks the event as lasting the whole day (or the whole span of days it covers) rather than a
    /// specific time range. An all-day event is rendered in the all-day row above the day and week
    /// time grids instead of being placed on the hour grid, and its badge shows no time.
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// Locks this single event: it cannot be dragged, resized, edited, or deleted even while the
    /// calendar itself is editable. Reading its details keeps working.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Draws the event as a shaded band behind the grid instead of a card - a lunch break, a holiday, a room under
    /// maintenance. It cannot be clicked, focused, dragged or edited, is left out of the agenda, the year view, the
    /// event lists and the "+N more" count, and does not count as an overlap. The day and week grids and the
    /// timeline draw its time range (on its <see cref="Resource"/>'s row, or on every row when it has none); the
    /// month grid tints the days it covers whole. Its title is spoken with the slots and days it covers. Combine it
    /// with <see cref="IsBlocking"/> to keep other events out of that time.
    /// </summary>
    public bool IsBackground { get; set; }

    /// <summary>
    /// Keeps every other event out of this event's time range: an add, a move or a resize that would overlap it is
    /// refused with <see cref="BitFullCalendarChangeRefusal.Blocked"/>, even while
    /// <see cref="BitFullCalendarSettings.AllowEventOverlap"/> is <c>true</c>. A blocking event with a
    /// <see cref="Resource"/> blocks only that resource; one without blocks them all. A blocking
    /// <see cref="IsBackground"/> band is drawn hatched.
    /// </summary>
    public bool IsBlocking { get; set; }

    /// <summary>
    /// Extra CSS class(es) applied to every element that renders this event - the month badge, the
    /// day/week block, the timeline block, and the agenda row. Use it to single an event out
    /// (for example a "tentative" hatch) without replacing the whole template.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Repeat rule that turns this event into a series master. The calendar expands it into
    /// occurrences across the visible range; this event itself is never rendered once it has one.
    /// <c>null</c> (the default) is a one-off event.
    /// </summary>
    public BitFullCalendarRecurrence? Recurrence { get; set; }

    /// <summary>
    /// Set on a generated occurrence to the <see cref="Id"/> of the series master it came from.
    /// <c>null</c> on every event a consumer supplied.
    /// </summary>
    public string? SeriesId { get; set; }

    /// <summary>
    /// Set on a generated occurrence to the date it falls on, so a click handler can tell which one
    /// of a series the user picked. <c>null</c> on every event a consumer supplied.
    /// </summary>
    public DateTime? OccurrenceDate { get; set; }

    /// <summary>True when this event was generated by expanding a recurrence rule.</summary>
    public bool IsOccurrence => SeriesId is not null;

    /// <summary>
    /// True when the event occupies a single date. An all-day event that spans one date is still
    /// single-day; the flag only affects where it is rendered, not how long it is.
    /// </summary>
    public bool IsSingleDay => StartDate.Date == BitFullCalendarHelpers.GetInclusiveEndDate(this);

    /// <summary>True when the event spans more than one date.</summary>
    public bool IsMultiDay => !IsSingleDay;

    /// <summary>
    /// True when the event belongs in the all-day row above the time grids: either it is explicitly
    /// marked <see cref="IsAllDay"/> or it spans more than one date.
    /// </summary>
    public bool IsAllDayOrMultiDay => IsAllDay || IsMultiDay;

    /// <summary>The difference between <see cref="EndDate"/> and <see cref="StartDate"/>.</summary>
    public TimeSpan Duration => EndDate - StartDate;

    /// <summary>
    /// Optional consumer-defined payload carried along to templates and callbacks.
    /// </summary>
    public object? Data { get; set; }
}
