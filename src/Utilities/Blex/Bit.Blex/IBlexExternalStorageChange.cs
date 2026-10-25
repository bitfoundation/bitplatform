namespace Bit.Blex;

/// <summary>
/// Implemented by an <see cref="IBlexStorage"/> that can tell Blex when a key was changed by
/// something <em>outside</em> this application instance -- most commonly another browser tab
/// writing to the same <c>localStorage</c>.
/// </summary>
/// <remarks>
/// <para>
/// Without this, two tabs of the same app quietly fight: each saves its own state after every
/// action, so whichever tab acts last wins and the other's changes vanish on its next reload. A
/// storage that raises <see cref="ExternalChange"/> lets
/// <see cref="BlexStatePersistor"/> re-read the affected store and apply it, so both tabs converge
/// on the same state instead.
/// </para>
/// <para>
/// The event carries the storage key, not the store name; the persistor maps it back using
/// <see cref="BlexPersistenceOptions.KeyPrefix"/>. A <c>null</c> key means "something changed, but
/// I don't know what" (browsers report this when storage is cleared wholesale) and re-reads every
/// persistent store.
/// </para>
/// <para>
/// Implementations must raise the event only for changes they did <em>not</em> make themselves;
/// echoing back this instance's own writes would spin the two sides against each other. The
/// browser's <c>storage</c> event already has this property -- it never fires in the tab that wrote
/// the value.
/// </para>
/// </remarks>
public interface IBlexExternalStorageChange
{
    /// <summary>
    /// Raised when a key was written or removed by another instance. The argument is the storage
    /// key, or <c>null</c> when the whole area changed.
    /// </summary>
    event Action<string?>? ExternalChange;

    /// <summary>
    /// Begins watching for external changes. Called once by <see cref="BlexStatePersistor"/> during
    /// startup, after hydration, so an implementation can defer its (possibly asynchronous) setup
    /// until persistence is actually in use. Safe to call more than once.
    /// </summary>
    ValueTask StartWatchingAsync(CancellationToken cancellationToken = default);
}
