namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class SignUpRequestDto
{
    /// <summary>
    /// The user's email
    /// </summary>
    /// <example>me@gmail.com</example>
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    /// <example>123456</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessage = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    /// <example>true</example>
    [Range(typeof(bool), "true", "true", ErrorMessage = nameof(AppStrings.YouHaveToAcceptTerms))]
    [Display(Name = nameof(AppStrings.TermsAccepted))]
    public bool TermsAccepted { get; set; }

    public string? GoogleRecaptchaToken { get; set; }
}
