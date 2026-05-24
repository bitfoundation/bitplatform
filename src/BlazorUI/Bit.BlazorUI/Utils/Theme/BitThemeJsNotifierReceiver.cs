namespace Bit.BlazorUI;

/// <summary>
/// JS-invokable bridge for <c>BitTheme.registerDotNetNotifier</c>; registered lazily by <see cref="BitThemeManager"/>.
/// </summary>
public sealed class BitThemeJsNotifierReceiver
{
    private readonly BitThemeNotifications _notifications;

    public BitThemeJsNotifierReceiver(BitThemeNotifications notifications)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        _notifications = notifications;
    }

    [JSInvokable]
    public void NotifyThemeChangedFromJs(string newTheme, string oldTheme)
    {
        _notifications.Raise(newTheme, oldTheme);
    }
}
