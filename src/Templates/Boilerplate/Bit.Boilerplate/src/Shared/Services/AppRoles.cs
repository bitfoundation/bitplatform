namespace Boilerplate.Shared.Services;

public class AppRoles
{
    /// <summary>
    /// Has all features <see cref="AppFeatures"/> automatically assigned (See IAuthTokenProvider.ReadClaims and AppJwtSecureDataFormat.Unprotect).
    /// </summary>
    public const string SuperAdmin = "s-admin";


    public const string Demo = "demo";

    public static bool IsBuiltInRole(string name)
    {
        return name is SuperAdmin or Demo;
    }
}
