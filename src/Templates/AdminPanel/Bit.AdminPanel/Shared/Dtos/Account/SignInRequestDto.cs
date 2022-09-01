
namespace AdminPanel.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class SignInRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_Invalid))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? UserName { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }
}
