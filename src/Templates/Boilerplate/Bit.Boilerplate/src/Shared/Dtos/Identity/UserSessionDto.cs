//+:cnd:noEmit
namespace Boilerplate.Shared.Dtos.Identity;

public partial class UserSessionDto
{
    public Guid Id { get; set; }

    public string? IP { get; set; }

    /// <summary>
    /// Populated during sign-in using the <see cref="SignInRequestDto.DeviceInfo"/> property.
    /// </summary>
    public string? DeviceInfo { get; set; }

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
    /// <summary>
    /// This property, in addition to the OS/Browser's detection of whether the user has permitted notifications,
    /// determines if the user is willing to receive notifications from the server.
    /// </summary>
    public bool NotificationsAllowed { get; set; }
    //#endif
}
