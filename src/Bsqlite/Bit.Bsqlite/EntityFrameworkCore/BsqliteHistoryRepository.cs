using Bit.Bsqlite.Data;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bit.Bsqlite.EntityFrameworkCore;

/// <summary>
/// A history repository whose migration lock keeps other tabs out without synchronous I/O.
/// </summary>
/// <remarks>
/// <para>
/// EF's SQLite history repository guards migrations with a lock row in a table, whose
/// <c>Dispose()</c> runs <c>ExecuteScalar</c> - synchronously, even on the <c>MigrateAsync</c> path.
/// That is the second and last blocker the S1 audit found.
/// </para>
/// <para>
/// The lock itself is still needed: every tab runs its own worker over the same OPFS or IndexedDB
/// file, and two tabs opening after a deploy would otherwise both read the history table, both find
/// the new migration pending, and the second would fail applying it again. So the lock comes from
/// the transport (<see cref="IBsqliteLockSource"/>) instead - a Web Lock in the browser, which the
/// browser drops when a tab closes mid-migration, where a lock row would be left behind for every
/// other tab to wait on. A transport that cannot coordinate tabs gets no lock.
/// </para>
/// <para>
/// The history-table SQL matches EF's SQLite provider: model-driven create script with
/// <c>IF NOT EXISTS</c> spliced in, and the same rejection of conditional script helpers SQLite
/// cannot express.
/// </para>
/// </remarks>
public sealed class BsqliteHistoryRepository(HistoryRepositoryDependencies dependencies)
    : HistoryRepository(dependencies)
{
    protected override string ExistsSql => $"""
        SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '{TableName}' AND "type" = 'table'
        """;

    protected override bool InterpretExistsResult(object? value)
        => value is not null && Convert.ToInt64(value) != 0L;

#if NET9_0_OR_GREATER
    // The migration lock arrived with EF Core 9; EF Core 8 takes none, so on net8.0 there is nothing to
    // replace - BsqliteDbContextFactory holds a lock of its own around the migration instead.

    /// <summary>
    /// The lock belongs to neither the connection nor a transaction - the browser holds it - so it
    /// lasts until EF releases it.
    /// </summary>
    public override LockReleaseBehavior LockReleaseBehavior => LockReleaseBehavior.Explicit;
#endif

    /// <summary>
    /// Reuses the model-driven create script so the table and column names stay in step with EF,
    /// and only splices in <c>IF NOT EXISTS</c> - which is what SQLite offers in place of the
    /// conditional blocks other providers use.
    /// </summary>
    public override string GetCreateIfNotExistsScript()
    {
        const string createTable = "CREATE TABLE";

        var script = GetCreateScript();
        var index = script.IndexOf(createTable, StringComparison.Ordinal);

        return index < 0
            ? script
            : script.Insert(index + createTable.Length, " IF NOT EXISTS");
    }

#if NET9_0_OR_GREATER
    /// <summary>The cross-tab lock migrations run under; see the remarks.</summary>
    internal const string MigrationLockName = "ef-migrations";

    /// <summary>Not supported: waiting for another tab cannot block the browser's main thread.</summary>
    public override IMigrationsDatabaseLock AcquireDatabaseLock()
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(
            nameof(AcquireDatabaseLock), nameof(AcquireDatabaseLockAsync));

    /// <summary>Waits until no other tab is migrating this database, then keeps them out until released.</summary>
    public override async Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(
        CancellationToken cancellationToken = default)
    {
        var held = Dependencies.Connection.DbConnection is BsqliteConnection connection
            ? await connection.TryAcquireLockAsync(MigrationLockName, cancellationToken).ConfigureAwait(false)
            : null;

        return new MigrationLock(this, held);
    }
#endif

    /// <summary>
    /// SQLite has no procedural <c>IF</c>, so EF's own provider rejects these too. They are only
    /// reached when generating idempotent SQL scripts, which is a server-side workflow.
    /// </summary>
    public override string GetBeginIfNotExistsScript(string migrationId)
        => throw new NotSupportedException(
            "SQLite does not support conditional migration scripts; generate a plain script instead.");

    public override string GetBeginIfExistsScript(string migrationId)
        => throw new NotSupportedException(
            "SQLite does not support conditional migration scripts; generate a plain script instead.");

    public override string GetEndIfScript()
        => throw new NotSupportedException(
            "SQLite does not support conditional migration scripts; generate a plain script instead.");

#if NET9_0_OR_GREATER
    private sealed class MigrationLock(IHistoryRepository historyRepository, IAsyncDisposable? held)
        : IMigrationsDatabaseLock
    {
        private IAsyncDisposable? _held = held;

        public IHistoryRepository HistoryRepository { get; } = historyRepository;

        /// <summary>
        /// EF releases from a synchronous <c>Dispose</c> even on the <c>MigrateAsync</c> path, so the
        /// release is started and not awaited, as a synchronously disposed transaction starts its
        /// rollback. Letting go of a Web Lock waits on nothing else this tab is doing.
        /// </summary>
        public void Dispose() => _ = ReleaseQuietlyAsync();

        public ValueTask DisposeAsync()
            => Interlocked.Exchange(ref _held, null)?.DisposeAsync() ?? ValueTask.CompletedTask;

        private async Task ReleaseQuietlyAsync()
        {
            try
            {
                await DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Nothing can observe this failure, and a host that failed has dropped its locks.
            }
        }
    }
#endif
}
