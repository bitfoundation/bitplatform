namespace Boilerplate.Shared.Dtos.Identity;

public partial class RoleClaimRequestDto
{
    public int Id { get; set; }

    public Guid RoleId { get; set; }

    public string? ClaimType { get; set; }

    public string? ClaimValue { get; set; }
}
