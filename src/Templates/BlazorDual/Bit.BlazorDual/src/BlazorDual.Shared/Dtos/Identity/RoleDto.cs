namespace BlazorDual.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class RoleDto
{
    public int Id { get; set; }

    public string? Name { get; set; }
}
