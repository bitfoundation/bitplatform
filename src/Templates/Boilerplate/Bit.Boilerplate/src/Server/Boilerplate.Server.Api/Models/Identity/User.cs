//+:cnd:noEmit
//#if (sample == true || offlineDb == true)
using Boilerplate.Server.Api.Models.Todo;
//#endif
using Boilerplate.Server.Api.Models.Attachments;

namespace Boilerplate.Server.Api.Models.Identity;

public partial class User : IdentityUser<Guid>
{
    [PersonalData]
    public string? FullName { get; set; }

    public string? DisplayName => FullName ?? DisplayUserName;
    public string? DisplayUserName => FullName ?? Email ?? PhoneNumber ?? UserName;

    [PersonalData]
    public Gender? Gender { get; set; }

    [PersonalData]
    public DateTimeOffset? BirthDate { get; set; }

    /// <summary>
    /// The date and time of the last token request. Ensures only the latest generated token is valid and can only be used once.
    /// </summary>
    public DateTimeOffset? EmailTokenRequestedOn { get; set; }

    public DateTimeOffset? PhoneNumberTokenRequestedOn { get; set; }

    public DateTimeOffset? ResetPasswordTokenRequestedOn { get; set; }

    public DateTimeOffset? TwoFactorTokenRequestedOn { get; set; }

    public DateTimeOffset? OtpRequestedOn { get; set; }

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.ELEVATED_ACCESS" />
    /// </summary>
    public DateTimeOffset? ElevatedAccessTokenRequestedOn { get; set; }

    public List<UserSession> Sessions { get; set; } = [];

    //#if (sample == true || offlineDb == true)
    public List<TodoItem> TodoItems { get; set; } = [];
    //#endif

    public List<WebAuthnCredential> WebAuthnCredentials { get; set; } = [];

    public List<UserRole> Roles { get; set; } = [];
    public List<UserClaim> Claims { get; set; } = [];
    public List<UserLogin> Logins { get; set; } = [];
    public List<UserToken> Tokens { get; set; } = [];

    public bool HasProfilePicture { get; set; }
}
