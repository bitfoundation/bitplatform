namespace Boilerplate.Shared.Services;

/// <summary>
/// Users with these roles will automatically receive assigned claims and permissions in IAuthTokenProvider.ReadClaims and AppJwtSecureDataFormat.Unprotect.
/// Other roles can be dynamically created by super admin users within the application's manage users and roles pages.
/// </summary>
public class AppBuiltInRoles
{
    /// <summary>
    /// Has all permissions <see cref="AppPermissions"/>
    /// </summary>
    public const string SuperAdmin = "s-admin";

    /// <summary>
    /// All permissions except <see cref="AppPermissions.Management"/>
    /// </summary>

    public const string BasicUser = "b-user";
}
