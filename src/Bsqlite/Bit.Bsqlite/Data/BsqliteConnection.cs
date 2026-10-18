using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bsqlite.Data;

/// <summary>
/// ADO.NET connection whose work is carried out by an <see cref="IBsqliteTransport"/>.
/// </summary>
/// <remarks>
/// Synchronous ADO.NET members throw <see cref="BsqliteSynchronousApiNotSupportedException"/>:
/// the transport is asynchronous and the browser main thread cannot block on it. The opt-in strict
/// tier exists to lift that restriction.
/// </remarks>
public sealed class BsqliteConnection : DbConnection
{
    private readonly IBsqliteTransport _transport;
    private readonly HashSet<string> _pendingTables = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _database;
    private readonly string _connectionString;
    private ConnectionState _state = ConnectionState.Closed;
    private bool _disposed;

    public BsqliteConnection(IBsqliteTransport transport, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        _transport = transport;
        _database = databaseName;
        _connectionString = $"Data Source={databaseName}";

        // A write in another tab reaches this connection only through the transport, and the
        // subscription has to exist before the first query so a live query created immediately
        // after opening does not miss one.
        _transport.TablesChanged += OnTransportTablesChanged;
    }

    internal IBsqliteTransport Transport => _transport;

    internal BsqliteTransaction? CurrentTransaction { get; set; }

    /// <summary>
    /// What the selected backend can honour. Defaults to unrestricted so an in-process transport
    /// used without a provider still behaves like a full SQLite.
    /// </summary>
    public BsqliteRuntimeLimits RuntimeLimits { get; init; } = BsqliteRuntimeLimits.Unrestricted;

    /// <summary>
    /// The rowid of the most recent successful <c>INSERT</c> executed through this connection, as
    /// SQLite's <c>last_insert_rowid()</c> reports it after the last command completed.
    /// </summary>
    public long LastInsertRowId { get; private set; }

    /// <summary>
    /// Raised after a committed write so live queries can re-run. Table-level: every listed name was
    /// touched by the write, locally or - when the transport can hear them - in another tab.
    /// </summary>
    public event EventHandler<BsqliteTablesChangedEventArgs>? TablesChanged;

    /// <summary>
    /// Raises <see cref="TablesChanged"/> for <paramref name="tables"/>, for an application that
    /// changed data in a way nothing else can see - a write on another connection, say.
    /// </summary>
    /// <remarks>
    /// The command layer does not need this: it reports its own writes through
    /// <see cref="ExecuteBatchAsync"/>, and writes from other tabs arrive through the transport.
    /// </remarks>
    public void NotifyTablesChanged(IEnumerable<string> tables)
    {
        ArgumentNullException.ThrowIfNull(tables);
        TablesChanged?.Invoke(this, new BsqliteTablesChangedEventArgs([.. tables]));
    }

    /// <summary>
    /// Called after a batch whose SQL writes has run, with the tables the SQL names.
    /// </summary>
    /// <remarks>
    /// Nothing happens when the transport reports its own writes - it knows better than the SQL
    /// text does, and it knows when the write is committed. Otherwise the tables are raised at once,
    /// unless a <see cref="BsqliteTransaction"/> is open, in which case they wait for its
    /// outcome: a live query that re-ran mid-transaction would show data that may yet roll back,
    /// and would compete with the writer for a <c>DbContext</c> that allows one operation at a
    /// time. Transactions driven by raw <c>BEGIN</c>/<c>COMMIT</c> text are not seen here, and a
    /// write inside one is reported as it happens.
    /// </remarks>
    internal void OnCommandWrote(IEnumerable<string> tables)
    {
        if (_transport.ReportsLocalWrites)
        {
            return;
        }

        if (CurrentTransaction is null)
        {
            NotifyTablesChanged(tables);
            return;
        }

        _pendingTables.UnionWith(tables);
    }

    /// <summary>Raises what the transaction that just committed had written.</summary>
    internal void CommitPendingWrites()
    {
        if (_pendingTables.Count == 0)
        {
            return;
        }

        var tables = new HashSet<string>(_pendingTables, StringComparer.OrdinalIgnoreCase);
        _pendingTables.Clear();
        TablesChanged?.Invoke(this, new BsqliteTablesChangedEventArgs(tables));
    }

    /// <summary>Forgets what a rolled-back transaction had written: nothing anyone can see changed.</summary>
    internal void DiscardPendingWrites() => _pendingTables.Clear();

    /// <summary>
    /// <c>Data Source=&lt;database&gt;</c>, for display. The database is fixed by the transport
    /// this connection was built on, so the string cannot be changed to point elsewhere.
    /// </summary>
    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            if (!string.Equals(value, _connectionString, StringComparison.Ordinal))
            {
                throw new NotSupportedException(
                    "A BsqliteConnection is bound to the database its transport opened; the "
                    + "connection string cannot be changed. Open another session for another database.");
            }
        }
    }

    public override string Database => _database;

    public override string DataSource => _database;

    /// <summary>The SQLite version of the vendored engine; see <c>engine/wa-sqlite.lock.props</c>.</summary>
    public override string ServerVersion => EntityFrameworkCore.BsqlitePclProvider.EngineVersion;

    public override ConnectionState State => _state;

    /// <summary>Batches are supported: one round trip runs every command in order.</summary>
    public override bool CanCreateBatch => true;

    public override void Open()
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(nameof(Open), nameof(OpenAsync));

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_state == ConnectionState.Open)
        {
            return;
        }

        _state = ConnectionState.Connecting;
        try
        {
            await _transport.OpenAsync(_database, cancellationToken).ConfigureAwait(false);
            _state = ConnectionState.Open;
        }
        catch
        {
            _state = ConnectionState.Closed;
            throw;
        }
    }

    /// <summary>
    /// Marks the connection closed without touching the transport.
    /// </summary>
    /// <remarks>
    /// <para>
    /// EF opens and closes a connection around every operation, while opening a SQLite database in
    /// the worker means setting up a VFS and reading page headers. Forwarding each ADO.NET close to
    /// the transport would pay that cost per query, so the transport deliberately keeps the database
    /// open and this is bookkeeping only. The transport's lifetime belongs to whoever created it,
    /// not to an individual connection.
    /// </para>
    /// <para>
    /// That also makes <see cref="Close"/> safe to leave synchronous. It is reached from
    /// <c>DbContext.Dispose()</c>, which is far too common to forbid, and blocking on an async close
    /// would deadlock the browser's single main thread - the exact failure this type exists to avoid.
    /// </para>
    /// </remarks>
    public override void Close() => _state = ConnectionState.Closed;

    public override Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Not supported: the transport owns exactly one database, so there is nothing to change to.
    /// Renaming here would only make <see cref="Database"/> disagree with the worker.
    /// </summary>
    public override void ChangeDatabase(string databaseName)
        => throw new NotSupportedException(
            "A BsqliteConnection is bound to the database its transport opened. Open another "
            + "session for another database.");

    /// <summary>Creates a command bound to this connection.</summary>
    public new BsqliteCommand CreateCommand() => new(this);

    /// <summary>Creates a batch bound to this connection.</summary>
    public new BsqliteBatch CreateBatch() => new(this);

    protected override DbCommand CreateDbCommand() => CreateCommand();

    protected override DbBatch CreateDbBatch() => CreateBatch();

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(
            nameof(BeginTransaction), nameof(BeginTransactionAsync));

    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        if (CurrentTransaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already active on this connection. SQLite does not nest "
                + "transactions; use savepoints (DbTransaction.SaveAsync) inside the active one.");
        }

        var transaction = new BsqliteTransaction(this, isolationLevel);
        await transaction.BeginAsync(cancellationToken).ConfigureAwait(false);
        CurrentTransaction = transaction;
        return transaction;
    }

    /// <summary>
    /// Reads the whole database out as a SQLite file image, consistent as of the last committed
    /// write, without closing the connection.
    /// </summary>
    /// <exception cref="NotSupportedException">The transport cannot snapshot an open database.</exception>
    public Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
        => _transport is IBsqliteSnapshotSource snapshots
            ? snapshots.ExportAsync(cancellationToken)
            : throw new NotSupportedException(
                $"{_transport.GetType().Name} cannot export an open database. Close the session and "
                + "use the storage provider's Admin.ExportAsync instead.");

    /// <summary>
    /// Runs a batch of requests in one transport round trip and returns one result per request.
    /// This is the lowest-level execution path; commands and batches are built on it.
    /// </summary>
    public async Task<IReadOnlyList<BsqliteCommandResult>> ExecuteBatchAsync(
        IReadOnlyList<BsqliteCommandRequest> batch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        // No check of State: Close is bookkeeping only (see its remarks), and a connection shared
        // with EF reads as closed between EF's operations while its transport stays open. Whether
        // the batch can run is the transport's call, and an unopened one says so.
        for (var i = 0; i < batch.Count; i++)
        {
            BsqliteFeatureGuards.EnsureSupported(batch[i].CommandText, RuntimeLimits);
        }

        IReadOnlyList<BsqliteCommandResult> results;
        try
        {
            results = await _transport.ExecuteAsync(batch, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Also on failure: the statements before the one that failed have run, and in
            // autocommit mode they have committed.
            ReportWrites(batch);
        }

        for (var i = 0; i < results.Count; i++)
        {
            if (results[i].LastInsertRowId != 0)
            {
                LastInsertRowId = results[i].LastInsertRowId;
            }
        }

        return results;
    }

    /// <summary>
    /// Derives the tables a batch wrote from its SQL, for a transport that cannot report them.
    /// </summary>
    private void ReportWrites(IReadOnlyList<BsqliteCommandRequest> batch)
    {
        if (_transport.ReportsLocalWrites)
        {
            return;
        }

        HashSet<string>? tables = null;
        for (var i = 0; i < batch.Count; i++)
        {
            if (BsqliteTableNames.LooksLikeWrite(batch[i].CommandText))
            {
                (tables ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase))
                    .UnionWith(BsqliteTableNames.Extract(batch[i].CommandText));
            }
        }

        if (tables is { Count: > 0 })
        {
            OnCommandWrote(tables);
        }
    }

    /// <summary>Runs a statement that returns nothing, bypassing command plumbing.</summary>
    internal async Task ExecuteInternalAsync(string sql, CancellationToken cancellationToken)
    {
        BsqliteFeatureGuards.EnsureSupported(sql, RuntimeLimits);
        var request = new BsqliteCommandRequest { CommandText = sql };
        await _transport.ExecuteAsync([request], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Unsubscribes from the transport. The transport itself is not disposed - it belongs to
    /// whoever created it, for the reason <see cref="Close"/> explains.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _transport.TablesChanged -= OnTransportTablesChanged;
        }

        base.Dispose(disposing);
    }

    private void OnTransportTablesChanged(object? sender, BsqliteTablesChangedEventArgs e)
        => TablesChanged?.Invoke(this, e);
}
