namespace Boilerplate.Shared.Dtos.Identity;

public partial class ToggleRolePermissionDto
{
    public Guid RoleId { get; set; }

    public string? Value { get; set; }

    public bool IsAdd { get; set; }
}
