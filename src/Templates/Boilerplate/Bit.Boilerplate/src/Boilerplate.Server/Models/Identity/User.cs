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

    public DateTimeOffset? EmailTokenRequestedOn { get; set; }

    public DateTimeOffset? PhoneNumberTokenRequestedOn { get; set; }

    public DateTimeOffset? ResetPasswordTokenRequestedOn { get; set; }

    public DateTimeOffset? TwoFactorTokenRequestedOn { get; set; }
}
