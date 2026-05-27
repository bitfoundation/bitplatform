using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// Raised when the global <c>bit-theme</c> document attribute changes (including OS-driven updates when following system theme).
/// Subscribe in scoped components; requires <see cref="BitThemeManager"/> interop at least once per circuit so the client script can notify .NET.
/// </summary>
/// <remarks>
/// <para>
/// Subscribers MUST unsubscribe when their component is disposed. <see cref="BitThemeNotifications"/>
/// is registered as a scoped service, so a leaked handler keeps the (potentially disposed) component
/// rooted for the lifetime of the circuit and produces re-renders on torn-down state.
/// </para>
/// <para>
/// Handlers are invoked on whichever thread <see cref="Raise"/> is called from (typically the JS
/// interop callback thread). If you need to update Blazor component state, marshal back through
/// <c>ComponentBase.InvokeAsync(StateHasChanged)</c>.
/// </para>
/// <para>
/// A throwing handler does not prevent the remaining handlers from running and is logged when an
/// <see cref="ILoggerFactory"/> is registered in DI.
/// </para>
/// </remarks>
public sealed class BitThemeNotifications
{
    private readonly ILogger<BitThemeNotifications>? _logger;

    public BitThemeNotifications()
    {
    }

    public BitThemeNotifications(ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<BitThemeNotifications>();
    }

    /// <summary>Fires after <c>BitTheme.set</c>, <c>toggleDarkLight</c>, or <c>prefers-color-scheme</c> updates while following system theme.</summary>
    public event EventHandler<BitThemeChangedEventArgs>? ThemeChanged;

    internal void Raise(string? newTheme, string? oldTheme)
    {
        var handler = ThemeChanged;
        if (handler is null) return;

        var args = new BitThemeChangedEventArgs(newTheme, oldTheme);

        // Invoke each subscriber in isolation. EventHandler.Invoke would short-circuit the
        // remaining delegates on the first throw, and any escaping exception from the JS-invoked
        // path can fault the circuit. Per-subscriber try/catch + log is the correct contract for
        // a notification service consumed by arbitrary user code.
        foreach (var subscriber in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<BitThemeChangedEventArgs>)subscriber).Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "BitThemeNotifications.ThemeChanged handler threw and was suppressed.");
            }
        }
    }
}
