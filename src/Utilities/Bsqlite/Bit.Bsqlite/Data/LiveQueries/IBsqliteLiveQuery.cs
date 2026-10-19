namespace Bit.Bsqlite.Data;

/// <summary>
/// A query that re-executes when any table it reads is written, locally or in another tab.
/// </summary>
/// <remarks>
/// Re-runs happen after a write is committed - a rolled-back write is never shown - and are
/// coalesced: a burst of writes produces one refresh reading the final state, not one per write. A
/// refresh that fails, typically because the <c>DbContext</c> it queries is still busy with the
/// write that triggered it, is retried once and then reported on <see cref="RefreshFailed"/> rather
/// than allowed to fault the writer; call <see cref="RefreshAsync"/> to read again explicitly.
/// </remarks>
public interface IBsqliteLiveQuery<T> : IAsyncDisposable
{
    /// <summary>Raised with the new result after every re-run, including an explicit refresh.</summary>
    event EventHandler<T>? Changed;

    /// <summary>
    /// Raised when a write-triggered re-run fails even after its retry. An explicit
    /// <see cref="RefreshAsync"/> throws instead.
    /// </summary>
    event EventHandler<Exception>? RefreshFailed;

    /// <summary>The result of the latest run, or <see langword="default"/> before the first.</summary>
    T? Current { get; }

    /// <summary>Runs the query now, publishes the result, and returns it.</summary>
    Task<T> RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The current result first, then the result of every later re-run, until
    /// <paramref name="cancellationToken"/> is cancelled or the query is disposed. A consumer that
    /// falls behind receives the newest result when it catches up, not each one it missed.
    /// </summary>
    IAsyncEnumerable<T> WithCancellation(CancellationToken cancellationToken);
}
