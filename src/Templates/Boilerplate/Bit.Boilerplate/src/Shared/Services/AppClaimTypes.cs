//+:cnd:noEmit
namespace Boilerplate.Shared.Services;

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
    /// true/false
    /// <inheritdoc cref="AuthPolicies.ELEVATED_ACCESS"/>
    /// </summary>
    public const string ELEVATED_SESSION = "e-s";

    /// <summary>
    /// Array: The list of Boilerplate app features (claims) assigned to the user.
    /// <see cref="AppFeatures"/>
    /// </summary>
    public const string FEATURES = "boilerplate-features";
}
