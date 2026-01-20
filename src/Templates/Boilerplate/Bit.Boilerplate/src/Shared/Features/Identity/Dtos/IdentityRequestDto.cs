namespace Boilerplate.Shared.Features.Identity.Dtos;

[DtoResourceType(typeof(AppStrings))]
public partial class IdentityRequestDto : IValidatableObject
{
    /// <example>"test"</example>
    [Display(Name = nameof(AppStrings.UserName))]
    public virtual string? UserName { get; set; }

    /// <example>"test@bitplatform.dev"</example>
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public virtual string? Email { get; set; }

    /// <example>"+31684207362"</example>
    [Phone(ErrorMessage = nameof(AppStrings.PhoneAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.PhoneNumber))]
    public virtual string? PhoneNumber { get; set; }

    /// <summary>
    /// URL for redirection after user confirmation, used when sending a confirmation email to an unconfirmed user.
    /// </summary>
    public virtual string? ReturnUrl { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(UserName) && string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(PhoneNumber))
        {
            yield return new ValidationResult(
                errorMessage: nameof(AppStrings.EitherProvideUserNameOrEmailOrPhoneNumber),
                memberNames: [nameof(UserName), nameof(Email), nameof(PhoneNumber)]
            );
        }
    }

    public override string? ToString()
    {
        return Email ?? PhoneNumber ?? UserName;
    }
}
