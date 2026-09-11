//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Identity.Dtos;

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

    //#if (signalR == true || notification == true)
    /// <summary>
    /// The device's own choice (See NotificationPreferenceService), so a new sign-in on it follows what it was set to.
    /// </summary>
    public UserSessionNotificationStatus NotificationStatus { get; set; }
    //#endif
}
