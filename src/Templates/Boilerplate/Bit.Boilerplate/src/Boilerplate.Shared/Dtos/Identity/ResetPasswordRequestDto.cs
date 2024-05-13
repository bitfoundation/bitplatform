//+:cnd:noEmit

namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class ResetPasswordRequestDto : IValidatableObject
{
    /// <example>bitplatform</example>
    [Display(Name = nameof(AppStrings.UserName))]
    public string? UserName { get; set; }

    /// <example>test@bitplatform.dev</example>
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    /// <example>+31684207362</example>
    [Phone(ErrorMessage = nameof(AppStrings.PhoneNumber))]
    [Display(Name = nameof(AppStrings.PhoneAttribute_Invalid))]
    public string? PhoneNumber { get; set; }

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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(UserName) && string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(PhoneNumber))
            yield return new ValidationResult(errorMessage: nameof(AppStrings.EitherProvideUserNameOrEmailOrPhoneNumber), [nameof(Email), nameof(PhoneNumber)]);
    }
}
