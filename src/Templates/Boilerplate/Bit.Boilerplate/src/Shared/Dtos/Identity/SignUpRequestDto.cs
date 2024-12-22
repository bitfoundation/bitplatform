//+:cnd:noEmit
namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public partial class SignUpRequestDto : IdentityRequestDto
{
    /// <example>test2</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.UserName))]
    public override string? UserName { get; set; }

    /// <example>123456</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessage = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    //#if (captcha == "reCaptcha")
    /// <example>null</example>
    public string? GoogleRecaptchaResponse { get; set; }
    //#endif

    /// <example>/</example>
    public string? ReturnUrl { get; set; } = Urls.HomePage;

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(PhoneNumber))
            yield return new ValidationResult(errorMessage: nameof(AppStrings.EitherProvideEmailOrPhoneNumber), [nameof(Email), nameof(PhoneNumber)]);
    }
}
