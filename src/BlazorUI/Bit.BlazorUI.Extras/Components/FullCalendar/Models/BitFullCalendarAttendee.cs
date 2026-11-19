namespace Bit.BlazorUI;

/// <summary>
/// A person taking part in a <see cref="BitFullCalendarEvent"/> (<see cref="BitFullCalendarEvent.Attendees"/>), shown on
/// the event's dialogs and offered by the calendar's attendee filter.
/// </summary>
public class BitFullCalendarAttendee
{
    /// <summary>The attendee's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The attendee's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// An optional identifier of the attendee. The attendee filter tells attendees apart by it, and by their
    /// <see cref="FullName"/> when it is not set, so two people with the same name need one each.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>The first and the last name, trimmed and joined by a space.</summary>
    public string FullName => string.Join(' ', new[] { FirstName?.Trim(), LastName?.Trim() }
        .Where(part => !string.IsNullOrEmpty(part)));

    /// <summary>The first letters of the first and the last name, in upper case: the text of the attendee's
    /// avatar.</summary>
    public string Initials
    {
        get
        {
            var firstName = FirstName?.Trim();
            var lastName = LastName?.Trim();
            var first = firstName?.Length > 0 ? char.ToUpperInvariant(firstName[0]).ToString() : "";
            var last = lastName?.Length > 0 ? char.ToUpperInvariant(lastName[0]).ToString() : "";
            return first + last;
        }
    }
}

