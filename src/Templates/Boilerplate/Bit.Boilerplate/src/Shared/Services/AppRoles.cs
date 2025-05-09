namespace Boilerplate.Shared.Services;

public class AppRoles
{
    /// <summary>
    /// Has all permissions <see cref="AppPermissions"/> automatically assigned (See IAuthTokenProvider.ReadClaims and AppJwtSecureDataFormat.Unprotect).
    /// </summary>
    public const string SuperAdmin = "s-admin";


    public const string Demo = "demo";
}
