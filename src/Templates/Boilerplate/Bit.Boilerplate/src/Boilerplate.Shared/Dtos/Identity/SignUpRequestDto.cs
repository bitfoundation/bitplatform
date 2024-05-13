//+:cnd:noEmit
namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class SignUpRequestDto : IValidatableObject
{
    /// <example>user</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.UserName))]
    public string? UserName { get; set; }

    /// <summary>
    /// The user's email
    /// </summary>
    /// <example>me@gmail.com</example>
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    /// <example>+31123456789</example>
    [Phone(ErrorMessage = nameof(AppStrings.PhoneNumber))]
    [Display(Name = nameof(AppStrings.PhoneAttribute_Invalid))]
    public string? PhoneNumber { get; set; }

    /// <example>123456</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessage = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    /// <example>true</example>
    [Range(typeof(bool), "true", "true", ErrorMessage = nameof(AppStrings.YouHaveToAcceptTerms))]
    [Display(Name = nameof(AppStrings.TermsAccepted))]
    public bool TermsAccepted { get; set; }

    //#if (captcha == "reCaptcha")
    public string? GoogleRecaptchaResponse { get; set; }
    //#endif

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(PhoneNumber))
            yield return new ValidationResult(errorMessage: nameof(AppStrings.EitherProvideEmailOrPhoneNumber), [nameof(Email), nameof(PhoneNumber)]);
    }
}
