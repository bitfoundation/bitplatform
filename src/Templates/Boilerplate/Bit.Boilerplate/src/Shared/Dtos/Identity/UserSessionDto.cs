namespace Boilerplate.Shared.Dtos.Identity;

public class UserSessionDto
{
    public Guid SessionUniqueId { get; set; }

    public string? IP { get; set; }

    public string? Device { get; set; }

    public string? Address { get; set; }

    public int LastSeenOn { get; set; }

    public string? LastSeenOnText { get; set; }

    public bool IsValid { get; set; }
}
