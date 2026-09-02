namespace Bit.Butil;

/// <summary>
/// Whether this origin may show system notifications.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification/permission_static">Notification.permission</see>
/// </summary>
public enum NotificationPermission
{
    /// <summary>
    /// The user refuses to have notifications displayed.
    /// </summary>
    Denied,

    /// <summary>
    /// The user accepts having notifications displayed.
    /// </summary>
    Granted,

    /// <summary>
    /// The user choice is unknown and therefore the browser will act as if the value were denied.
    /// </summary>
    Default
}
