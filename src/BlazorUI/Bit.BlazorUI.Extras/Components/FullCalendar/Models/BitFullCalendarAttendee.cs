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
            var first = FirstName?.Length > 0 ? char.ToUpperInvariant(FirstName[0]).ToString() : "";
            var last = LastName?.Length > 0 ? char.ToUpperInvariant(LastName[0]).ToString() : "";
            return first + last;
        }
    }
}

