namespace Boilerplate.Server.Api.Models.Identity;

public partial class UserSession
{
    public Guid Id { get; set; }

    public string? IP { get; set; }

    public string? Device { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset StartedOn { get; set; }

    public DateTimeOffset? RenewedOn { get; set; }

    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
