namespace Bit.BlazorUI;

/// <summary>
/// Represents an event to be displayed on a calendar day.
/// </summary>
public class BitCalendarEvent
{
    /// <summary>
    /// The title of the event.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// The full body/description of the event.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// The date on which the event occurs.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The optional start time of the event.
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// The optional end time of the event.
    /// </summary>
    public TimeOnly? EndTime { get; set; }
}
