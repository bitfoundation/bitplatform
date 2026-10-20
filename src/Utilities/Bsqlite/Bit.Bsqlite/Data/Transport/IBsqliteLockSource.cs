namespace Bit.Bsqlite.Data;

/// <summary>
/// A transport that can hold a named lock shared with every other tab that opens the same database.
/// </summary>
/// <remarks>
/// <para>
/// Optional, like <see cref="IBsqliteSnapshotSource"/>. SQLite's own locks last one transaction at
/// most, and some work has to keep other tabs out for longer: applying EF migrations reads the history
/// table and then changes the schema, and two tabs doing that at once both see the same migrations as
/// pending.
/// </para>
/// <para>
/// The worker transport takes the lock through the Web Locks API, which the browser releases when the
/// tab holding it closes or crashes - so a lock is never left behind for the other tabs to wait on.
/// </para>
/// </remarks>
public interface IBsqliteLockSource
{
    /// <summary>
    /// Waits until nobody else holds <paramref name="name"/> for the open database, then takes it.
    /// Disposing the result releases it.
    /// </summary>
    Task<IAsyncDisposable> AcquireLockAsync(string name, CancellationToken cancellationToken = default);
}
