namespace Boilerplate.Shared.Dtos.Identity;

public partial class SendNotificationToRoleDto
{
    public Guid RoleId { get; set; }

    public string? Message { get; set; }

    public string? PageUrl { get; set; }
}
