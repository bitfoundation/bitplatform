using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// A transport factory that can hold a lock on a database's name before any transport is open, shared with
/// every other tab that opens it.
/// </summary>
/// <remarks>
/// Taken around an open that may migrate the database to another backend: resolving where it lives, copying
/// it, flipping the binding and opening it are separate steps, and two tabs running them at once would both
/// copy the database, one tab's import landing over the other's.
/// </remarks>
internal interface IBsqliteOpenLock
{
    /// <summary>
    /// Waits until no other tab is opening <paramref name="databaseName"/>, then holds that until the result is
    /// disposed. Null where there is nothing to coordinate with.
    /// </summary>
    /// <param name="databaseName">The database about to be opened.</param>
    /// <param name="boundProvider">
    /// Reads the backend the database is bound to now, or null when none is. The lock is taken before the open
    /// resolves where the database lives, so this is the only word on the backend there is yet: a factory that
    /// locks only for some backends asks it, one that always locks need not.
    /// </param>
    /// <param name="cancellationToken">Gives up waiting.</param>
    Task<IAsyncDisposable?> AcquireOpenLockAsync(
        string databaseName,
        Func<CancellationToken, ValueTask<IBsqliteStorageProvider?>> boundProvider,
        CancellationToken cancellationToken);
}
