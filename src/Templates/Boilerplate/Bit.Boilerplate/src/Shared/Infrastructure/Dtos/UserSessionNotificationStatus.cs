namespace Boilerplate.Shared.Infrastructure.Dtos;

/// <summary>
/// Whether a session receives notifications - push and SignalR's in-app SHOW_MESSAGE alike. The device reports it on
/// every UpdateSession from the switch in AppMenu, so a session only follows that device's own choice.
/// </summary>
public enum UserSessionNotificationStatus
{
    /// <summary>
    /// The default: a session is muted until its device opts in, which also keeps OAuth sessions muted for good.
    /// </summary>
    Muted,

    Allowed
}
