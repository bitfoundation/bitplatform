namespace Boilerplate.Shared.Dtos.Identity;

public partial class UpdateUserSessionRequestDto
{
    /// <example>"Samsung Android 14"</example>
    public string? DeviceInfo { get; set; }

    public AppPlatformType PlatformType { get; set; }

    /// <summary>
    /// The version of that application that the user is using in this session.
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// The culture selected by the user in this session.
    /// </summary>
    public string? CultureName { get; set; }
}
