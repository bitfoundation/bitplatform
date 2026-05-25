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
        // Defensive normalization: parameters are non-nullable in the .NET signature, but the
        // values originate from JS where null/undefined can slip through. Normalize so subscribers
        // never observe null and the contract on BitThemeChangedEventArgs.NewTheme/OldTheme holds.
        _notifications.Raise(newTheme ?? string.Empty, oldTheme ?? string.Empty);
    }
}
