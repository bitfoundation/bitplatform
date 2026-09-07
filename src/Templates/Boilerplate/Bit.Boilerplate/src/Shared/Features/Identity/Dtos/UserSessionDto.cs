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
    /// <summary>
    /// The most recently connected tab or app of this session, not all of them. See <c>UserSession.SignalRConnectionId</c>.
    /// </summary>
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

    /// <summary>
    /// Set when this session belongs to an external app authorized over OAuth. Surfaced in the sessions list because a
    /// grant to somebody else's software is what a user most needs to recognise before revoking it.
    /// </summary>
    public string? OAuthClientId { get; set; }

    /// <summary>The application's own name, where it gave one. Untrusted text: render as text, never as markup.</summary>
    public string? OAuthClientName { get; set; }

    /// <summary>Space delimited, as granted - what this application may actually do, rather than merely that it can.</summary>
    public string? OAuthScope { get; set; }
}
