namespace Bit.BlazorUI;

/// <summary>
/// A schedulable resource shown as a row in the resource timeline view (for example,
/// a meeting room, a person, a piece of equipment).
/// Events are linked to a resource through <see cref="BitFullCalendarEvent.Resource"/>
/// matching <see cref="Id"/>.
/// </summary>
public sealed class BitFullCalendarResource
{
    /// <summary>
    /// Stable identifier matched against <see cref="BitFullCalendarEvent.Resource"/>.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Display name for the resource (for example "Bay Wing", "Alice Johnson", "Meeting Room 3B").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional subtitle shown below the resource title (for example building, department).
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Optional consumer-defined payload available to templates and click handlers.
    /// </summary>
    public object? Data { get; set; }
}
