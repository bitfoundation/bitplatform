namespace Boilerplate.Shared.Features.Identity.Dtos;

[DtoResourceType(typeof(AppStrings))]
public partial class RoleDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? Name { get; set; }

    public string? NormalizedName { get; set; }
}
