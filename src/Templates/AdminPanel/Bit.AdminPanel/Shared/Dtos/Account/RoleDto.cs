using AdminPanel.Shared.Attributes;

namespace AdminPanel.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class RoleDto
{
    public int Id { get; set; }

    public string? Name { get; set; }
}
