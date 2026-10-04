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
    /// The color of the indicator dot the event puts on its day. Without one the dot takes the color of the
    /// calendar itself, so a calendar whose events are all of a kind needs to say nothing here.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The optional start time of the event.
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// The optional end time of the event.
    /// </summary>
    public TimeOnly? EndTime { get; set; }
}
