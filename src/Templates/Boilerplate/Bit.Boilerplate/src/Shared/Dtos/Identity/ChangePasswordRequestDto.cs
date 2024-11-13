namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public partial class ChangePasswordRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessage = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.OldPassword))]
    public string? OldPassword { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessage = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.NewPassword))]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Compare(nameof(NewPassword), ErrorMessage = nameof(AppStrings.CompareAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.ConfirmNewPassword))]
    public string? ConfirmPassword { get; set; }
}
