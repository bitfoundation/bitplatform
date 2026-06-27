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
    /// Cannot be null, empty, or whitespace - grouping helpers key rows by this value and assume a
    /// non-empty key, so a blank id is rejected at assignment time. Ids must also be unique across
    /// the resource list assigned to the calendar; duplicate ids are rejected when the resources are
    /// applied because timeline row grouping and rendering key on this value.
    /// </summary>
    public required string Id
    {
        get => _id;
        set => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Resource Id cannot be null, empty, or whitespace.", nameof(value))
            : value;
    }
    private string _id = null!;

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
