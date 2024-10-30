namespace Boilerplate.Shared.Dtos.Identity;

public partial class UserSessionDto
{
    public Guid SessionUniqueId { get; set; }

    public string? IP { get; set; }

    public string? Device { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset RenewedOn { get; set; }

    public bool IsValid { get; set; }
}
