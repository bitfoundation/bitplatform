namespace Boilerplate.Shared.Features.Identity.Dtos;

[DtoResourceType(typeof(AppStrings))]
public partial class ClaimDto
{
    public string? ClaimType { get; set; }

    public string? ClaimValue { get; set; }
}
