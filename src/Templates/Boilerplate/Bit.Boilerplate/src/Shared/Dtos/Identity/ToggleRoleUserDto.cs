namespace Boilerplate.Shared.Dtos.Identity;

public partial class ToggleRoleUserDto
{
    public Guid RoleId { get; set; }

    public Guid UserId { get; set; }

    public bool IsAdd { get; set; }
}
