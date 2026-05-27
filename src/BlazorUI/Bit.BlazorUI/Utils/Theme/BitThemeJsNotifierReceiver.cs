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
    public void NotifyThemeChangedFromJs(string? newTheme, string? oldTheme)
    {
        // Defensive normalization: these values originate from JS, so null/undefined can slip
        // through (hence the nullable .NET signature). Coalesce to empty strings so subscribers
        // never observe null and the non-null contract on BitThemeChangedEventArgs.NewTheme/OldTheme holds.
        _notifications.Raise(newTheme ?? string.Empty, oldTheme ?? string.Empty);
    }
}
