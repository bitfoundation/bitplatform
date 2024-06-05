namespace Boilerplate.Server.Models.Identity;

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
    /// Confirms or changes email. Ensures only the latest token is valid and single-use.
    /// </summary>
    public DateTimeOffset? EmailTokenRequestedOn { get; set; }

    /// <summary>
    /// Confirms or changes phone number. Ensures only the latest token is valid and single-use.
    /// </summary>
    public DateTimeOffset? PhoneNumberTokenRequestedOn { get; set; }

    public DateTimeOffset? ResetPasswordTokenRequestedOn { get; set; }

    public DateTimeOffset? TwoFactorTokenRequestedOn { get; set; }

    /// <summary>
    /// Sign in with OTP or magic link. Ensures only the latest token is valid and single-use.
    /// </summary>
    public DateTimeOffset? OtpRequestedOn { get; set; }
}
