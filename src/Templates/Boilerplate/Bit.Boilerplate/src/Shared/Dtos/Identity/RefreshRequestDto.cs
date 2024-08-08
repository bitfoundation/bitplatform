namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class RefreshRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? RefreshToken { get; set; }
}