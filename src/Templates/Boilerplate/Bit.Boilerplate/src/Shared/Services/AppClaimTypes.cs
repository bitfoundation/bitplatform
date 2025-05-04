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

public class AppBuiltinRoles
{
    /// <summary>
    /// This role has all the permissions (claims).
    /// </summary>
    public const string SuperAdmin = "s-admin";

    /// <summary>
    /// The role of the users who sign-up using the app.
    /// </summary>
    public const string BasicUser = "b-user";

    // Note: The rest of the roles can be dynamically created by the super admin users in the app.
}
