namespace Bit.BlazorUI;

public class BitFullCalendarAttendee
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Id { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

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

