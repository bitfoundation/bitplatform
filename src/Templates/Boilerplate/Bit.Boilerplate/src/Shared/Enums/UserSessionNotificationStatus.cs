namespace Boilerplate.Shared.Enums;

/// <summary>
/// Represents the user's preference and status for receiving push notifications on specific devices/sessions.
/// </summary>
public enum UserSessionNotificationStatus
{
    /// <summary>
    /// Indicates that the user has not yet enabled or allowed push notifications.
    /// This typically means the user has not granted permission at the system or app level.
    /// </summary>
    NotConfigured,

    /// <summary>
    /// Indicates that the user has enabled push notifications and is willing to receive them.
    /// Notifications are permitted and actively delivered to the user.
    /// </summary>
    Allowed,

    /// <summary>
    /// Indicates that the user has enabled push notifications but has temporarily opted out of receiving them.
    /// </summary>
    Muted
}
