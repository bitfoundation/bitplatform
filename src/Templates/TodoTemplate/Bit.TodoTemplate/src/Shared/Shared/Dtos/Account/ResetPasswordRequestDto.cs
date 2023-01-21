namespace TodoTemplate.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class ResetPasswordRequestDto
{
    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessageResourceName = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? Token { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessageResourceName = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [NotMapped]
    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Compare(nameof(Password), ErrorMessageResourceName = nameof(AppStrings.CompareAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.ConfirmNewPassword))]
    public string? ConfirmPassword { get; set; }
}
