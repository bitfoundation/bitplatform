//+:cnd:noEmit
namespace Boilerplate.Shared.Infrastructure.Services;

public class AuthPolicies
{
    /// <summary>
    /// Having this policy/claim in access token means the user is allowed to view pages that require privileged access.
    /// Currently, this policy applies only to the Todo and AdminPanel specific pages like dashboard page. 
    /// However, it can be extended to cover additional pages as needed. 
    /// 
    /// By default, each user is limited to 3 active sessions.
    /// The user's max privileged sessions' value is stored in <see cref="AppClaimTypes.MAX_PRIVILEGED_SESSIONS"/> claim.
    /// 
    /// Important: Do not apply this policy to the settings page, as users need access to manage and revoke their sessions there.
    /// </summary>
    public const string PRIVILEGED_ACCESS = nameof(PRIVILEGED_ACCESS);

    /// <summary>
    /// Enables the user to execute potentially harmful operations, like account deletion.
    /// This limited-time policy is activated upon successful verification via a secure 6-digit code or
    /// during the initial minutes of a sign-in session of users with 2fa enabled.
    /// The elevated window is stored as the expiry timestamp in the <see cref="AppClaimTypes.ELEVATED_SESSION"/> claim and
    /// checked against the current time, so it survives refresh token calls (e.g. tenant switches) until it naturally expires.
    /// </summary>
    public const string ELEVATED_ACCESS = nameof(ELEVATED_ACCESS);

    //#if (multitenant == true)
    /// <summary>
    /// Determines whether the signed-in user currently selected a tenant,
    /// meaning the access token contains the <see cref="AppClaimTypes.TENANT_ID"/> claim.
    /// </summary>
    public const string TENANT_SELECTED = nameof(TENANT_SELECTED);
    //#endif
}
