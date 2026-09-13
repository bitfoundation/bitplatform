using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// Raised when the global <c>bit-theme</c> document attribute changes (including OS-driven updates when following system theme).
/// Subscribing to <see cref="ThemeChanged"/> on a DI-resolved instance automatically registers the JS notifier,
/// so no explicit <see cref="BitThemeManager"/> call is needed first.
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
    private readonly object _gate = new();
    private readonly ILogger<BitThemeNotifications>? _logger;

    private EventHandler<BitThemeChangedEventArgs>? _themeChanged;

    public BitThemeNotifications()
    {
    }

    public BitThemeNotifications(ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<BitThemeNotifications>();
    }

    /// <summary>
    /// Set by the DI factory in <see cref="IBitBlazorUIServiceCollectionExtensions"/> so that attaching a
    /// <see cref="ThemeChanged"/> handler kicks off <see cref="BitThemeManager"/>'s JS notifier registration -
    /// subscribing alone is enough to start receiving events, without a prior manager call.
    /// A lazy callback (rather than an injected <see cref="BitThemeManager"/>) because a constructor
    /// dependency would be circular: manager -> receiver -> notifications.
    /// </summary>
    internal Func<ValueTask>? RegistrationTrigger { get; set; }

    /// <summary>Fires after <c>BitBlazorUI.Theme.set</c>, <c>toggleDarkLight</c>, or <c>prefers-color-scheme</c> updates while following system theme.</summary>
    public event EventHandler<BitThemeChangedEventArgs>? ThemeChanged
    {
        add
        {
            lock (_gate)
            {
                _themeChanged += value;
            }

            // Triggered on every subscribe, not just the first: registration legitimately no-ops
            // during prerendering / on a disconnected circuit, so a later subscriber may be the
            // first one attaching in a state where it can succeed. Once registered, the trigger
            // short-circuits on the manager's fast-path flag.
            TriggerJsNotifierRegistration();
        }
        remove
        {
            lock (_gate)
            {
                _themeChanged -= value;
            }
        }
    }

    // Deliberately async void: this is a fire-and-forget kick from an event accessor (which cannot
    // await), and every failure path is caught below so nothing can escape to the SynchronizationContext.
    private async void TriggerJsNotifierRegistration()
    {
        var trigger = RegistrationTrigger;
        if (trigger is null) return;

        try
        {
            await trigger().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // The service scope or the manager is tearing down; there is nothing left to register.
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Automatic theme notifier registration on ThemeChanged subscribe failed.");
        }
    }

    internal void Raise(string? newTheme, string? oldTheme)
    {
        var handler = _themeChanged;
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
