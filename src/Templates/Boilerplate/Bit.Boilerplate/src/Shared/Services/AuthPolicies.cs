namespace Boilerplate.Shared.Services;

public class AuthPolicies
{
    /// <summary>
    /// Having this policy means the user has enabled 2 factor authentication.
    /// </summary>
    public const string TFA_ENABLED = nameof(TFA_ENABLED);

    /// <summary>
    /// By default, each user is limited to 3 active sessions.
    /// This policy can be disabled or configured to adjust the session limit dynamically, 
    /// such as by reading from application settings, the user's subscription plan, or other criteria.
    /// Currently, this policy applies only to the Todo and AdminPanel related pages. 
    /// However, it can be extended to cover additional pages as needed. 
    /// 
    /// Important: Do not apply this policy to the settings page, as users need access to manage and revoke their sessions there.
    /// </summary>
    public const string PRIVILEGED_ACCESS = nameof(PRIVILEGED_ACCESS);

    /// <summary>
    /// Enables the user to execute potentially harmful operations, like account removal. 
    /// This limited-time policy is activated upon successful verification via a secure 6-digit code or
    /// during the initial minutes of a sign-in session.
    /// </summary>
    public const string ELEVATED_ACCESS = nameof(ELEVATED_ACCESS);
}

public class AppClaimTypes
{
    public const string SESSION_ID = "session-id";

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    public const string PRIVILEGED_SESSION = "privileged-session";

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.ELEVATED_ACCESS"/>
    /// </summary>
    public const string ELEVATED_SESSION = "elevated-session";

    /// <summary>
    /// Refresh tokens are tied to specific user sessions, 
    /// This ensures that refresh tokens are single-use, preventing users from reusing a copied token to bypass the potential limit 
    /// on the maximum number of concurrent privileged sessions of the user.
    /// </summary>
    public const string SESSION_STAMP = "session_stamp";
}
