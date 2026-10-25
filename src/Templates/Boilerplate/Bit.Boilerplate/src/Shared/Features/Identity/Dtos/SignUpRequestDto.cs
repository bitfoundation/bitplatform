//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Identity.Dtos;

[DtoResourceType(typeof(AppStrings))]
public partial class SignUpRequestDto : IdentityRequestDto
{
    /// <example>"test2"</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.UserName))]
    public override string? UserName { get; set; }

    /// <example>"12345678"</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(8, ErrorMessage = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    //#if (captcha == "reCaptcha")
    /// <example>null</example>
    public string? GoogleRecaptchaResponse { get; set; }
    //#endif

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Exactly one: an unconfirmed second identifier lets its owner take over the account once the first is confirmed.
        if (string.IsNullOrWhiteSpace(Email) == string.IsNullOrWhiteSpace(PhoneNumber))
            yield return new ValidationResult(errorMessage: nameof(AppStrings.EitherProvideEmailOrPhoneNumber), [nameof(Email), nameof(PhoneNumber)]);
    }
}
