namespace Boilerplate.Shared.Services;

/// <summary>
/// These claims may not be added to RoleClaims/UserClaims tables.
/// The system itself will assign these claims to the user based on <see cref="AuthPolicies"/> policies.
/// </summary>
public class AppClaimTypes
{
    public const string SESSION_ID = "s-id";

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    public const string PRIVILEGED_SESSION = "p-s";

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// Maximum privileged sessions for the user.
    /// </summary>
    public const string MAX_PRIVILEGED_SESSIONS = "mx-p-s";

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.ELEVATED_ACCESS"/>
    /// </summary>
    public const string ELEVATED_SESSION = "e-s";

    /// <summary>
    /// The list of features (claims) assigned to the user.
    /// <see cref="AppFeatures"/>
    /// </summary>
    public const string FEATURES = "feat";
}
