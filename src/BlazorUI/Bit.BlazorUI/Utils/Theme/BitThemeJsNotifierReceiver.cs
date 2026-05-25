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
        // Defensive normalization: parameters are nullable in the .NET signature, so the
        // values originated from JS where null/undefined can slip through would not cause any issues. 
        // Normalize so subscribers never observe null and the contract on BitThemeChangedEventArgs.NewTheme/OldTheme holds.
        _notifications.Raise(newTheme ?? string.Empty, oldTheme ?? string.Empty);
    }
}
