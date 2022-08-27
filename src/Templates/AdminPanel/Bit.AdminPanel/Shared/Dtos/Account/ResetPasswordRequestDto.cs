
namespace AdminPanel.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_Invalid))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? Token { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessage = nameof(AppStrings.PasswordTooShort))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [NotMapped]
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Compare(nameof(Password), ErrorMessage = nameof(AppStrings.CompareAttribute_MustMatch))]
    public string? ConfirmPassword { get; set; }
}
