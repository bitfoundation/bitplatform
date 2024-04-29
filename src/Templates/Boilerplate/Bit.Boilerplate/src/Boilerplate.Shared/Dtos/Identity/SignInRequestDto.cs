//+:cnd:noEmit
namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class SignInRequestDto
{
    /// <example>test@bitplatform.dev</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? UserName { get; set; }

    /// <example>123456</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [JsonIgnore]
    [Display(Name = nameof(AppStrings.RememberMe))]
    public bool RememberMe { get; set; } = true;

    [Display(Name = nameof(AppStrings.TwoFactorCode))]
    public string? TwoFactorCode { get; set; }

    [Display(Name = nameof(AppStrings.TwoFactorRecoveryCode))]
    public string? TwoFactorRecoveryCode { get; set; }
}
