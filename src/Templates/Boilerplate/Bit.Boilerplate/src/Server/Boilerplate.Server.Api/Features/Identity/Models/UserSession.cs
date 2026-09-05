//+:cnd:noEmit
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif
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

    //#if (multitenant == true)
    /// <summary>
    /// The tenant the session is currently signed into. It gets updated whenever the user switches into another tenant.
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public Guid? TenantId { get; set; }
    //#endif

    //#if (notification == true)
    public PushNotificationSubscription? PushNotificationSubscription { get; set; }
    //#endif

    //#if (signalR == true)
    /// <summary>
    /// The SignalR connection of the tab or app that connected MOST RECENTLY on this session - not all of them.
    /// <para>
    /// A user session is one sign-in on one device, but the user can open the app several times on that device:
    /// several browser tabs of the same profile, or the Windows exe started more than once. Each of those builds
    /// its own SignalR connection while reading the same access token, so they all report the same session id and
    /// each one overwrites this column as it connects. The last writer wins, and the earlier tabs stay open and
    /// signed in with no way for the server to address them through this column.
    /// </para>
    /// <para>
    /// So anything sent here reaches ONE tab or app. That is fine for what it is used for - a device-level action
    /// the user is watching for (an AI chatbot tool acting on the device) - but do not treat it as "notify this
    /// session". For that, target all the sessions of the user instead.
    /// </para>
    /// </summary>
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
