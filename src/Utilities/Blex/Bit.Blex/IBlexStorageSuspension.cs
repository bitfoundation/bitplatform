namespace Bit.Blex;

/// <summary>
/// Implemented by an <see cref="IBlexStorage"/> that lives on a page which can go away without the
/// application being disposed -- a browser tab that is closed, reloaded, navigated away from or
/// discarded in the background. Lets <see cref="BlexStatePersistor"/> write a debounced save before
/// the page is gone.
/// </summary>
/// <remarks>
/// Without this, a change made within <see cref="BlexPersistenceOptions.DebounceInterval"/> of a reload
/// is lost: Blazor WebAssembly never disposes the app when its page unloads, so the persistor's own
/// flush on disposal never runs, and under a steady stream of changes up to
/// <see cref="BlexPersistenceOptions.DebounceMaxDelay"/> of them is lost with it.
/// </remarks>
public interface IBlexStorageSuspension
{
    /// <summary>
    /// Raised when the page is being hidden or unloaded, and the host may not run anything once the
    /// handler returns. Handlers must do their work synchronously; the browser storage raises it from
    /// a synchronous call wherever the host allows one (WebAssembly).
    /// </summary>
    event Action? Suspending;

    /// <summary>
    /// Begins watching for the page being hidden or unloaded. Called once by
    /// <see cref="BlexStatePersistor"/> during startup. Safe to call more than once.
    /// </summary>
    ValueTask StartWatchingSuspensionAsync(CancellationToken cancellationToken = default);
}
