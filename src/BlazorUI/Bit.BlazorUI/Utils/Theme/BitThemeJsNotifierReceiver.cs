using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// JS-invokable bridge for <c>BitTheme.registerDotNetNotifier</c>; registered lazily by <see cref="BitThemeManager"/>.
/// Internal because consumers should subscribe to <see cref="BitThemeNotifications.ThemeChanged"/>
/// instead of constructing this type directly.
/// </summary>
internal sealed class BitThemeJsNotifierReceiver
{
    private readonly BitThemeNotifications _notifications;
    private readonly ILogger<BitThemeJsNotifierReceiver>? _logger;

    public BitThemeJsNotifierReceiver(BitThemeNotifications notifications)
        : this(notifications, loggerFactory: null)
    {
    }

    public BitThemeJsNotifierReceiver(BitThemeNotifications notifications, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        _notifications = notifications;
        _logger = loggerFactory?.CreateLogger<BitThemeJsNotifierReceiver>();
    }

    [JSInvokable]
    public Task NotifyThemeChangedFromJs(string? newTheme, string? oldTheme)
    {
        // newTheme/oldTheme arrive verbatim from JS — they may be null when the document has no
        // bit-theme attribute. We pass them through; BitThemeChangedEventArgs treats null as
        // "unknown" without coercing to an empty-string sentinel.
        try
        {
            _notifications.Raise(newTheme, oldTheme);
        }
        catch (Exception ex)
        {
            // Raise() already isolates per-subscriber failures; this catch-all only triggers if
            // BitThemeNotifications itself faults. Swallow + log so a JS->.NET round-trip never
            // tears the circuit, mirroring the JS-side `.catch` on invokeMethodAsync.
            _logger?.LogError(ex, "BitThemeJsNotifierReceiver.NotifyThemeChangedFromJs failed.");
        }

        // Returning Task (instead of void) lets the JS side observe completion via
        // invokeMethodAsync and surfaces any unexpected sync throw as a faulted task rather
        // than as an unobserved exception in the SynchronizationContext.
        return Task.CompletedTask;
    }
}
