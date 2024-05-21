﻿//+:cnd:noEmit

namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class SignInRequestDto : IdentityRequestDto
{
    /// <example>123456</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [JsonIgnore]
    [Display(Name = nameof(AppStrings.RememberMe))]
    public bool RememberMe { get; set; } = true;

    /// <summary>
    /// Two factor code generated by authenticator app
    /// </summary>
    /// <example>null</example>
    [Display(Name = nameof(AppStrings.TwoFactorCode))]
    public string? TwoFactorCode { get; set; }

    /// <summary>
    /// Two factor token received by email or sms
    /// </summary>
    /// <example>null</example>
    [Display(Name = nameof(AppStrings.TwoFactorToken))]
    public string? TwoFactorToken { get; set; }

    /// <example>null</example>
    [Display(Name = nameof(AppStrings.TwoFactorRecoveryCode))]
    public string? TwoFactorRecoveryCode { get; set; }
}
