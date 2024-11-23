namespace Boilerplate.Shared.Services;

public class AuthPolicies
{
    /// <summary>
    /// Having this policy means the user has enabled 2 factor authentication.
    /// </summary>
    public const string TFA_ENABLED = nameof(TFA_ENABLED);

    /// <summary>
    /// Enables the user to execute potentially harmful operations, like account removal. 
    /// This limited-time policy is activated upon successful verification via a secure 6-digit code or
    /// during the initial minutes of a sign-in session.
    /// </summary>
    public const string PRIVILEGED_ACCESS = nameof(PRIVILEGED_ACCESS);
}

public class AppClaimTypes
{
    public const string SESSION_ID = "session-id";

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    public const string PRIVILEGED_SESSION = "privileged-session";
}
