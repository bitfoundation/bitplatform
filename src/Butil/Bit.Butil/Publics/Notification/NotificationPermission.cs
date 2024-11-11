namespace Bit.Butil;

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
