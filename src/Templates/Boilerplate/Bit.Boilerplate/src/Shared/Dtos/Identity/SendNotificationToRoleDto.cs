namespace Boilerplate.Shared.Dtos.Identity;

public partial class SendNotificationToRoleDto
{
    public Guid RoleId { get; set; }

    public string? Message { get; set; }

    /// <summary>
    /// The page to be opened when the notification is clicked.
    /// </summary>
    public string? PageUrl { get; set; }
}
