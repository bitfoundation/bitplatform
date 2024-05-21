﻿namespace Boilerplate.Server.Models.Identity;

public class User : IdentityUser<int>
{
    [PersonalData]
    public string? FullName { get; set; }

    public string? DisplayName => FullName ?? Email ?? PhoneNumber ?? UserName;

    [PersonalData]
    public Gender? Gender { get; set; }

    [PersonalData]
    public DateTimeOffset? BirthDate { get; set; }

    [PersonalData]
    public string? ProfileImageName { get; set; }

    /// <summary>
    /// To either confirm and/or change email
    /// </summary>
    public DateTimeOffset? EmailTokenRequestedOn { get; set; }

    /// <summary>
    /// To either confirm and/or change phone number
    /// </summary>
    public DateTimeOffset? PhoneNumberTokenRequestedOn { get; set; }

    public DateTimeOffset? ResetPasswordTokenRequestedOn { get; set; }

    public DateTimeOffset? TwoFactorTokenRequestedOn { get; set; }

    /// <summary>
    /// To sign in with either Otp or magic link.
    /// </summary>
    public DateTimeOffset? OtpRequestedOn { get; set; }
}
