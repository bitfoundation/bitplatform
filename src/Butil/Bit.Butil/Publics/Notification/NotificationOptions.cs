namespace Bit.Butil;

/// <summary>
/// An options object containing any custom settings that you want to apply to the notification.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// A string containing the URL of the image used to represent the notification when there isn't enough space to display the notification itself
    /// </summary>
    public string? Badge { get; set; }

    /// <summary>
    /// A string representing the body text of the notification, which is displayed below the title.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Arbitrary data that you want associated with the notification.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// The direction in which to display the notification. It defaults to auto, which just adopts the browser's language setting behavior.
    /// </summary>
    public NotificationDirection Dir { get; set; }

    /// <summary>
    /// A string containing the URL of an icon to be displayed in the notification.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// A string containing the URL of an image to be displayed in the notification.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// The notification's language, as specified using a string representing a language tag
    /// </summary>
    public string? Lang { get; set; }

    /// <summary>
    /// A boolean value specifying whether the user should be notified after a new notification replaces an old one.
    /// </summary>
    public bool? Renotify { get; set; }

    /// <summary>
    /// Indicates that a notification should remain active until the user clicks or dismisses it, rather than closing automatically.
    /// </summary>
    public bool RequireInteraction { get; set; }

    /// <summary>
    /// A boolean value specifying whether the notification should be silent
    /// </summary>
    public bool? Silent { get; set; }

    /// <summary>
    /// A string representing an identifying tag for the notification.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// A timestamp, given as Unix time in milliseconds, representing the time associated with the notification.
    /// </summary>
    public long? Timestamp { get; set; }

    /// <summary>
    /// A vibration pattern for the device's vibration hardware to emit with the notification.
    /// </summary>
    public int[]? Vibrate { get; set; }
}
