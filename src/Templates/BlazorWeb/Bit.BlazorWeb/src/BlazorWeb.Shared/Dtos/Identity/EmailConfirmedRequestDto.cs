
namespace BlazorWeb.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class EmailConfirmedRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }
}
