//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Identity.Dtos;

public partial class UserSessionDto
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
    public long RenewedOn { get; set; }

    public DateTimeOffset RenewedOnDateTimeOffset
    {
        get
        {
            // Unix epoch starts at 1970-01-01 00:00:00 UTC
            DateTimeOffset epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            // Add the Unix timestamp (in seconds) to the epoch
            return epoch.AddSeconds(RenewedOn);
        }
    }

    //#if (signalR == true)
    public string? SignalRConnectionId { get; set; }
    //#endif

    //#if (signalR == true || notification == true)
    public UserSessionNotificationStatus NotificationStatus { get; set; }
    //#endif

    public string? DeviceInfo { get; set; }

    /// <summary>
    /// The culture selected by the user for this session.
    /// </summary>
    public string? CultureName { get; set; }

    /// <summary>
    /// The version of the application used for this session.
    /// </summary>
    public string? AppVersion { get; set; }
}
