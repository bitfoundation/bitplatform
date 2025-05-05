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
    /// <inheritdoc cref="AuthPolicies.ELEVATED_ACCESS"/>
    /// </summary>
    public const string ELEVATED_SESSION = "e-s";

    /// <summary>
    /// The list of permissions (claims) assigned to the user.
    /// <see cref="AppPermissions"/>
    /// </summary>
    public const string PERMISSIONS = "per";
}
