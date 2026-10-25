using System.Collections.Concurrent;
using System.Data;
using System.Runtime.InteropServices;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// An <see cref="IBsqliteTransport"/> that runs the native SQLite engine in the application's own
/// process, through Microsoft.Data.Sqlite.
/// </summary>
/// <remarks>
/// <para>
/// This is the transport Bsqlite uses where .NET runs natively - Blazor Hybrid - for a backend that
/// implements <see cref="IBsqliteFileStorageProvider"/>. It carries out exactly what the worker does in
/// the browser: it installs the function set EF Core's SQLite provider expects, reports committed
/// writes through SQLite's update and commit hooks, snapshots through <c>sqlite3_serialize</c>, and
/// holds named locks shared with every other transport on the same database. So everything above it -
/// the ADO.NET surface, EF Core, live queries - behaves the same on a device as in a browser tab.
/// </para>
/// <para>
/// Writes committed through one transport are reported to every other transport open on the same
/// database in this process, the way a write in one browser tab reaches the others: a Blazor Hybrid
/// app with two windows keeps its live queries current in both.
/// </para>
/// <para>
/// Batches run one at a time, since a SQLite connection serves one caller at once - the worker queues
/// its messages for the same reason. A transport opened for a file storage provider also runs them on
/// the thread pool, as the worker keeps SQLite off the browser's main thread, so a Blazor Hybrid UI
/// thread never waits on the disk.
/// </para>
/// </remarks>
public class BsqliteNativeTransport : IBsqliteTransport, IBsqliteSnapshotSource, IBsqliteLockSource
{
    // Transports over the same database share their locks and their change notifications, the way tabs
    // over the same file share the worker transport's Web Locks and BroadcastChannel.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SharedLocks = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, List<BsqliteNativeTransport>> SharedPeers = new(StringComparer.Ordinal);

    private readonly Func<string, string> _connectionStringFor;
    private readonly IReadOnlyList<string> _onOpenSql;
    private readonly bool _registerEfFunctions;
    private readonly bool _offload;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly HashSet<string> _writtenTables = new(StringComparer.OrdinalIgnoreCase);
    private ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);
    private string _scope = string.Empty;
    private bool _shared;
    private SqliteConnection? _connection;
    private bool _committed;

    // Held so the native side never calls a delegate the collector has reclaimed.
    private delegate_update? _updateHook;
    private delegate_commit? _commitHook;

    /// <summary>Opens the database <paramref name="connectionString"/> names, whatever name it is opened under.</summary>
    /// <param name="connectionString">A Microsoft.Data.Sqlite connection string.</param>
    /// <param name="onOpenSql">Statements to run on every open, after the function set is installed.</param>
    public BsqliteNativeTransport(string connectionString, IReadOnlyList<string>? onOpenSql = null)
        : this(connectionString, onOpenSql, registerEfFunctions: true)
    {
    }

    /// <summary>
    /// Opens the database <paramref name="connectionString"/> names, optionally without the EF function
    /// set - for tests that demonstrate what breaks without it.
    /// </summary>
    protected BsqliteNativeTransport(string connectionString, IReadOnlyList<string>? onOpenSql, bool registerEfFunctions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionStringFor = _ => connectionString;
        _onOpenSql = onOpenSql ?? [];
        _registerEfFunctions = registerEfFunctions;

        // Known now, so another transport's lock on the same database is honoured before this one opens.
        ConfigureScope(new SqliteConnectionStringBuilder(connectionString));
    }

    /// <summary>
    /// Opens each database as the file <paramref name="provider"/> keeps it in.
    /// </summary>
    /// <param name="provider">Where the databases live.</param>
    /// <param name="enableForeignKeys">Whether every connection runs with foreign keys enforced.</param>
    /// <param name="onOpenSql">Statements to run on every open, after the function set is installed.</param>
    public BsqliteNativeTransport(
        IBsqliteFileStorageProvider provider,
        bool enableForeignKeys = true,
        IReadOnlyList<string>? onOpenSql = null)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _connectionStringFor = databaseName => new SqliteConnectionStringBuilder
        {
            DataSource = provider.GetDatabasePath(databaseName),
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = enableForeignKeys,
            // Closing must release the file: the admin surface deletes, imports and exports it while
            // the database is closed, and a pooled handle would keep it open behind its back.
            Pooling = false,
        }.ToString();
        _onOpenSql = onOpenSql ?? [];
        _registerEfFunctions = true;
        _offload = true;
    }

    /// <summary>The underlying connection, once open, for callers that need to inspect engine state directly.</summary>
    public SqliteConnection? Connection => _connection;

    /// <inheritdoc />
    public event EventHandler<BsqliteTablesChangedEventArgs>? TablesChanged;

    /// <inheritdoc />
    /// <remarks>
    /// True, the way the worker transport is: writes are reported from SQLite's update hook once the
    /// transaction that made them has committed.
    /// </remarks>
    public bool ReportsLocalWrites => true;

    /// <summary>Called before each request in a batch runs.</summary>
    protected virtual void OnExecuting(BsqliteCommandRequest request)
    {
    }

    /// <inheritdoc />
    public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        if (_connection is not null)
        {
            return Task.CompletedTask;
        }

        var options = new SqliteConnectionStringBuilder(_connectionStringFor(databaseName));
        ConfigureScope(options);

        if (_shared && options.Mode != SqliteOpenMode.Memory
            && Path.GetDirectoryName(_scope) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        var connection = new SqliteConnection(options.ToString());
        try
        {
            connection.Open();

            if (_offload)
            {
                LeaveWalMode(connection);
            }

            if (_registerEfFunctions)
            {
                BsqliteNativeFunctions.Register(connection);
            }

            foreach (var sql in _onOpenSql)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
        catch
        {
            connection.Dispose();
            throw;
        }

        _connection = connection;

        // The same two hooks the worker installs: exact table names per row change, and the moment
        // they became visible. See NotifyIfCommitted for how they combine.
        var handle = connection.Handle!;
        _updateHook = (_, _, _, table, _) => _writtenTables.Add(table.utf8_to_string());
        _commitHook = _ =>
        {
            _committed = true;
            return raw.SQLITE_OK;
        };
        raw.sqlite3_update_hook(handle, _updateHook, null);
        raw.sqlite3_commit_hook(handle, _commitHook, null);

        if (_shared)
        {
            var peers = SharedPeers.GetOrAdd(_scope, _ => []);
            lock (peers)
            {
                peers.Add(this);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        // Closing an in-memory database would discard it, and EF opens and closes connections freely
        // around operations. The handle lives until disposal, which is what the owning session does.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
        IReadOnlyList<BsqliteCommandRequest> batch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var connection = _connection ?? throw new InvalidOperationException("The transport is not open.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _offload
                ? await Task.Run(() => ExecuteBatch(connection, batch, cancellationToken), cancellationToken).ConfigureAwait(false)
                : ExecuteBatch(connection, batch, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private List<BsqliteCommandResult> ExecuteBatch(
        SqliteConnection connection,
        IReadOnlyList<BsqliteCommandRequest> batch,
        CancellationToken cancellationToken)
    {
        var results = new List<BsqliteCommandResult>(batch.Count);
        try
        {
            foreach (var request in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                OnExecuting(request);

                var changesBefore = raw.sqlite3_total_changes(connection.Handle!);
                results.Add(Execute(connection, request) with
                {
                    LastInsertRowId = raw.sqlite3_last_insert_rowid(connection.Handle!),
                });
                RecordTextualWrites(request.CommandText, raw.sqlite3_total_changes(connection.Handle!) - changesBefore);
            }
        }
        finally
        {
            // Also on failure: the statements before the one that failed have run, and in autocommit
            // mode they have committed.
            NotifyIfCommitted(connection);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
    {
        var connection = _connection ?? throw new InvalidOperationException("The transport is not open.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return Serialize(connection);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static byte[] Serialize(SqliteConnection connection)
    {
        var pointer = raw.sqlite3_serialize(connection.Handle!, "main", out var size, 0);
        if (pointer == IntPtr.Zero)
        {
            if (size == 0)
            {
                return [];
            }

            throw new BsqliteException("sqlite3_serialize could not snapshot the database.");
        }

        try
        {
            var image = new byte[size];
            Marshal.Copy(pointer, image, 0, image.Length);
            return image;
        }
        finally
        {
            raw.sqlite3_free(pointer);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// A semaphore per database and name, shared by every transport in the process that opens the same
    /// file or shared-cache memory database - the in-process counterpart of a Web Lock.
    /// </remarks>
    public async Task<IAsyncDisposable> AcquireLockAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var gate = _locks.GetOrAdd($"{_scope}|{name}", _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new HeldLock(gate);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_shared && SharedPeers.TryGetValue(_scope, out var peers))
        {
            lock (peers)
            {
                peers.Remove(this);
            }
        }

        _connection?.Dispose();
        _connection = null;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Takes a file that is in WAL mode back to a rollback journal - the mode Bsqlite runs every database in -
    /// so its committed data is all in the one file the admin surface exports and imports.
    /// </summary>
    /// <remarks>
    /// A file arrives in WAL mode when a stock EF Core application created it: Bit.Besql's database on a
    /// device, typically. The switch checkpoints the log into the file; it needs the file to itself, so while
    /// another window holds it open the file is left as it is and the next open tries again.
    /// </remarks>
    private static void LeaveWalMode(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode";
        if (!string.Equals(command.ExecuteScalar() as string, "wal", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            command.CommandText = "PRAGMA journal_mode = DELETE";
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is raw.SQLITE_BUSY or raw.SQLITE_LOCKED)
        {
        }
    }

    /// <summary>
    /// Works out who this transport shares its database with: nobody for a private in-memory database,
    /// which is reachable through this transport alone, and otherwise every transport on the same file
    /// or shared-cache memory database.
    /// </summary>
    private void ConfigureScope(SqliteConnectionStringBuilder options)
    {
        var isPrivate = options.DataSource is "" or ":memory:"
            || (options.Mode == SqliteOpenMode.Memory && options.Cache != SqliteCacheMode.Shared);

        _shared = !isPrivate;
        _locks = isPrivate ? new(StringComparer.Ordinal) : SharedLocks;
        _scope = options.Mode == SqliteOpenMode.Memory || isPrivate
            ? "memory:" + options.DataSource
            : Path.GetFullPath(options.DataSource);
    }

    /// <summary>
    /// Adds the tables a request's SQL names - the same rule as the worker's <c>recordTextualWrites</c>.
    /// </summary>
    /// <remarks>
    /// The update hook is exact for row changes but blind to DDL and to the truncate-optimised
    /// <c>DELETE FROM t</c>, so the statement text fills those in - only when the request changed rows
    /// or reads as DDL, so a write that matched nothing raises nothing.
    /// </remarks>
    private void RecordTextualWrites(string sql, int changes)
    {
        if ((changes > 0 || BsqliteTableNames.LooksLikeSchemaChange(sql))
            && BsqliteTableNames.LooksLikeWrite(sql))
        {
            _writtenTables.UnionWith(BsqliteTableNames.Extract(sql));
        }
    }

    /// <summary>
    /// Raises <see cref="TablesChanged"/> for what was written, once it is committed - the same rule as
    /// the worker's <c>notifyIfCommitted</c> - here and on every other transport open on the database.
    /// </summary>
    /// <remarks>
    /// Nothing is raised while a transaction is open; once the connection is back in autocommit the
    /// accumulated names go out if a commit happened, and are dropped if the transaction rolled back.
    /// </remarks>
    private void NotifyIfCommitted(SqliteConnection connection)
    {
        if (raw.sqlite3_get_autocommit(connection.Handle!) == 0)
        {
            return;
        }

        var committed = _committed;
        var tables = new HashSet<string>(_writtenTables, StringComparer.OrdinalIgnoreCase);
        _committed = false;
        _writtenTables.Clear();

        if (!committed || tables.Count == 0)
        {
            return;
        }

        TablesChanged?.Invoke(this, new BsqliteTablesChangedEventArgs(tables));

        if (_shared && SharedPeers.TryGetValue(_scope, out var peers))
        {
            BsqliteNativeTransport[] others;
            lock (peers)
            {
                others = [.. peers.Where(peer => !ReferenceEquals(peer, this))];
            }

            // Raised off this call, as another tab's writes arrive in the browser: a peer's live query
            // re-runs on its own connection, and must not do so inside this one's ExecuteAsync.
            foreach (var other in others)
            {
                _ = Task.Run(() => other.TablesChanged?.Invoke(other, new BsqliteTablesChangedEventArgs(tables)));
            }
        }
    }

    private static BsqliteCommandResult Execute(SqliteConnection connection, BsqliteCommandRequest request)
    {
        using var command = connection.CreateCommand();
        command.CommandText = request.CommandText;

        foreach (var parameter in request.Parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }

        switch (request.ResultKind)
        {
            case BsqliteResultKind.NonQuery:
                return new BsqliteCommandResult { RecordsAffected = command.ExecuteNonQuery() };

            case BsqliteResultKind.Scalar:
            {
                var value = command.ExecuteScalar();
                return new BsqliteCommandResult
                {
                    ColumnNames = ["value"],
                    ColumnTypes = [null],
                    Rows = [[value is DBNull ? null : value]],
                };
            }

            case BsqliteResultKind.Reader:
                using (var reader = command.ExecuteReader(
                    request.RowLimit == 0 ? CommandBehavior.SchemaOnly : CommandBehavior.Default))
                {
                    return Materialize(reader, request.RowLimit ?? int.MaxValue);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.ResultKind, "Unknown result kind.");
        }
    }

    private static BsqliteCommandResult Materialize(SqliteDataReader reader, int rowLimit)
    {
        var columnNames = new string[reader.FieldCount];
        var columnTypes = new string?[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columnNames[i] = reader.GetName(i);
            columnTypes[i] = SafeDataTypeName(reader, i);
        }

        var rows = new List<object?[]>();
        while (rows.Count < rowLimit && reader.Read())
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        // Remaining statements in the text still have to run, as they do in the worker.
        while (reader.NextResult())
        {
        }

        return new BsqliteCommandResult
        {
            ColumnNames = columnNames,
            ColumnTypes = columnTypes,
            Rows = rows,
            RecordsAffected = reader.RecordsAffected,
        };
    }

    private static string? SafeDataTypeName(SqliteDataReader reader, int ordinal)
    {
        // Computed columns have no declared type and Microsoft.Data.Sqlite throws rather than returning null.
        try
        {
            return reader.GetDataTypeName(ordinal);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private sealed class HeldLock(SemaphoreSlim gate) : IAsyncDisposable
    {
        private int _released;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}
