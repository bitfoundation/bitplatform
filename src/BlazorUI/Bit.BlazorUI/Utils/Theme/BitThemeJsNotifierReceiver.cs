namespace Bit.BlazorUI;

/// <summary>
/// JS-invokable bridge for <c>BitTheme.registerDotNetNotifier</c>; registered lazily by <see cref="BitThemeManager"/>.
/// </summary>
public sealed class BitThemeJsNotifierReceiver : IDisposable
{
    private readonly BitThemeNotifications _notifications;

    public BitThemeJsNotifierReceiver(BitThemeNotifications notifications)
    {
        _notifications = notifications;
    }

    [JSInvokable]
    public void NotifyThemeChangedFromJs(string newTheme, string oldTheme)
    {
        _notifications.Raise(newTheme, oldTheme);
    }

    public void Dispose()
    {
    }
}
