//+:cnd:noEmit
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Server.Api.Features.PushNotification;

public class PushNotificationSubscription
{
    public int Id { get; set; }

    //#if (multitenant == true)
    /// <summary>
    /// The tenant this device last subscribed from, so that a broadcast can reach a device with no
    /// <see cref="UserSession"/> to carry one - which is what an anonymous visitor's row is.
    /// </summary>
    public Guid TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }
    //#endif

    [Required]
    public string? DeviceId { get; set; }

    [Required, AllowedValues("apns", "fcmV1", "browser")]
    public string? Platform { get; set; }

    [Required]
    public string? PushChannel { get; set; }

    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? Endpoint { get; set; }

    public Guid? UserSessionId { get; set; }

    [ForeignKey(nameof(UserSessionId))]
    public UserSession? UserSession { get; set; }

    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Unix Time Seconds
    /// </summary>
    public long ExpirationTime { get; set; }

    /// <summary>
    /// Unix Time Seconds
    /// </summary>
    public long RenewedOn { get; set; }
}
