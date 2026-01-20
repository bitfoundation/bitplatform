//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif

namespace Boilerplate.Server.Api.Features.Identity.Models;

public partial class UserSession
{
    public Guid Id { get; set; }

    public string? IP { get; set; }

    public string? Address { get; set; }

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    public bool Privileged { get; set; }

    /// <summary>
    /// Unix Time Seconds
    /// </summary>
    public long StartedOn { get; set; }

    /// <summary>
    /// Unix Time Seconds
    /// </summary>
    public long? RenewedOn { get; set; }

    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    //#if (notification == true)
    public PushNotificationSubscription? PushNotificationSubscription { get; set; }
    //#endif

    //#if (signalR == true)
    public string? SignalRConnectionId { get; set; }
    //#endif

    //#if (signalR == true || notification == true)
    public UserSessionNotificationStatus NotificationStatus { get; set; }
    //#endif

    public string? DeviceInfo { get; set; }

    public AppPlatformType? PlatformType { get; set; }

    /// <summary>
    /// The culture selected by the user for this session.
    /// </summary>
    public string? CultureName { get; set; }

    /// <summary>
    /// The version of the application used for this session.
    /// </summary>
    public string? AppVersion { get; set; }
}
