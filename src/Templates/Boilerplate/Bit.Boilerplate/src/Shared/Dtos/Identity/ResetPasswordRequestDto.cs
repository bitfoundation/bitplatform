//+:cnd:noEmit

namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public partial class ResetPasswordRequestDto : IdentityRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? Token { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessage = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Compare(nameof(Password), ErrorMessage = nameof(AppStrings.CompareAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.ConfirmNewPassword))]
    public string? ConfirmPassword { get; set; }
}
