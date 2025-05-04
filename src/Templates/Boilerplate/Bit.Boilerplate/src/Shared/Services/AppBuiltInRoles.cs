namespace Boilerplate.Shared.Services;

/// <summary>
/// Users with these roles will automatically receive assigned claims and permissions in IAuthTokenProvider.ReadClaims and AppJwtSecureDataFormat.Unprotect.
/// Other roles can be dynamically created by super admin users within the application's manage users and roles pages.
/// </summary>
public class AppBuiltInRoles
{
    public const string SuperAdmin = "s-admin";
    public const string BasicUser = "b-user";
}
