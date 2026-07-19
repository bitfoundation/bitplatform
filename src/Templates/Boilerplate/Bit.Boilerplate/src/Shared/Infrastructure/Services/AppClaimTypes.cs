//+:cnd:noEmit
namespace Boilerplate.Shared.Infrastructure.Services;

public class AppClaimTypes
{
    public const string SESSION_ID = "s-id";

    /// <summary>
    /// true/false
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    public const string PRIVILEGED_SESSION = "p-s";

    /// <summary>
    /// Number: Maximum privileged sessions for the user.
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    public const string MAX_PRIVILEGED_SESSIONS = "mx-p-s";

    /// <summary>
    /// Unix time seconds: the moment until which the session stays elevated (stored like the JWT's exp claim).
    /// The session is considered elevated as long as the current time hasn't passed this value, so a stale
    /// (already-passed) value is harmless and can be safely carried across refresh token calls.
    /// <inheritdoc cref="AuthPolicies.ELEVATED_ACCESS"/>
    /// </summary>
    public const string ELEVATED_SESSION = "e-s";

    /// <summary>
    /// Array: The list of Boilerplate app features (claims) assigned to the user.
    /// <see cref="AppFeatures"/>
    /// </summary>
    public const string FEATURES = "features";

    /// <summary>
    /// The method used for user authentication.
    /// External (Social), Sms (Web-OTP), Email (Magic Link or 6 digit code), Push notification (6 digit code), WebAuthn (Face-Id, Fingerprint etc), Password.
    /// </summary>
    public const string METHOD = "method";

    //#if (multitenant == true)
    /// <summary>
    /// Guid: The id of the tenant the user is currently signed into (if any).
    /// </summary>
    public const string TENANT_ID = "t-id";
    //#endif
}
