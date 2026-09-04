namespace Bit.Butil;

/// <summary>
/// Which way a notification's title and body are laid out.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification/dir">Notification.dir</see>
/// </summary>
public enum NotificationDirection
{
    /// <summary>Follow the browser's own locale. The default.</summary>
    Auto,

    /// <summary>Left to right.</summary>
    Ltr,

    /// <summary>Right to left.</summary>
    Rtl
}
