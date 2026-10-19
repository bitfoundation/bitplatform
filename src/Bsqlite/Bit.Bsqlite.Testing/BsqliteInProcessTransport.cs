using System.Collections.Concurrent;
using System.Data;
using System.Runtime.InteropServices;
using Bit.Bsqlite.Data;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Bit.Bsqlite.Testing;

/// <summary>
/// An <see cref="IBsqliteTransport"/> that runs SQLite in-process via Microsoft.Data.Sqlite.
/// </summary>
/// <remarks>
/// Stands in for the web-worker transport so provider behaviour can be tested on desktop .NET.
/// It mirrors the worker's responsibilities exactly - installing the EF function set (see
/// <see cref="BsqliteFunctions"/>), reporting committed writes through the same update-hook
/// and commit-hook mechanism, and snapshotting through <c>sqlite3_serialize</c> - so a test that
/// passes here is a meaningful signal.
/// </remarks>
public sealed class BsqliteInProcessTransport : IBsqliteTransport, IBsqliteSnapshotSource, IBsqliteLockSource
{
    // Transports over the same database share their locks, the way tabs over the same file share the
    // worker transport's Web Locks.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SharedLocks = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;
    private readonly string _lockScope;
    private readonly string _connectionString;
    private readonly bool _registerEfFunctions;
    private readonly HashSet<string> _writtenTables = new(StringComparer.OrdinalIgnoreCase);
    private SqliteConnection? _connection;
    private bool _committed;

    // Held so the native side never calls a delegate the collector has reclaimed.
    private delegate_update? _updateHook;
    private delegate_commit? _commitHook;

    /// <param name="connectionString">
    /// Defaults to a private in-memory database that lives as long as the connection.
    /// </param>
    /// <param name="registerEfFunctions">
    /// When <see langword="false"/>, the EF function set is deliberately omitted so tests can
    /// demonstrate what breaks without it.
    /// </param>
    public BsqliteInProcessTransport(
        string? connectionString = null,
        bool registerEfFunctions = true)
    {
        _connectionString = connectionString ?? "Data Source=:memory:";
        _registerEfFunctions = registerEfFunctions;

        // A private in-memory database is reachable through this transport alone, so there is nobody
        // to share its locks with.
        var options = new SqliteConnectionStringBuilder(_connectionString);
        var isPrivate = options.DataSource is "" or ":memory:"
            || (options.Mode == SqliteOpenMode.Memory && options.Cache != SqliteCacheMode.Shared);

        _locks = isPrivate ? new(StringComparer.Ordinal) : SharedLocks;
        _lockScope = options.Mode == SqliteOpenMode.Memory || isPrivate
            ? "memory:" + options.DataSource
            : Path.GetFullPath(options.DataSource);
    }

    /// <summary>Statements executed so far, in order. Useful for asserting generated SQL.</summary>
    public List<string> ExecutedCommands { get; } = [];

    /// <summary>The underlying connection, for tests that need to inspect engine state directly.</summary>
    public SqliteConnection? Connection => _connection;

    /// <inheritdoc />
    public event EventHandler<BsqliteTablesChangedEventArgs>? TablesChanged;

    /// <inheritdoc />
    /// <remarks>
    /// True, the way the worker transport is: writes are reported from SQLite's update hook once
    /// the transaction that made them has committed, so live queries behave on desktop exactly as
    /// they do in the browser.
    /// </remarks>
    public bool ReportsLocalWrites => true;

    public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
        {
            return Task.CompletedTask;
        }

        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        if (_registerEfFunctions)
        {
            BsqliteFunctions.Register(_connection);
        }

        // The same two hooks the worker installs: exact table names per row change, and the
        // moment they became visible. See NotifyIfCommitted for how they combine.
        var handle = _connection.Handle!;
        _updateHook = (_, _, _, table, _) => _writtenTables.Add(table.utf8_to_string());
        _commitHook = _ =>
        {
            _committed = true;
            return raw.SQLITE_OK;
        };
        raw.sqlite3_update_hook(handle, _updateHook, null);
        raw.sqlite3_commit_hook(handle, _commitHook, null);

        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        // Closing an in-memory database would discard it, and EF opens and closes connections
        // freely around operations. Keep it alive until disposal.
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
        IReadOnlyList<BsqliteCommandRequest> batch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (_connection is null)
        {
            throw new InvalidOperationException("The transport is not open.");
        }

        var results = new List<BsqliteCommandResult>(batch.Count);
        try
        {
            foreach (var request in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecutedCommands.Add(request.CommandText);

                var changesBefore = raw.sqlite3_total_changes(_connection.Handle!);
                results.Add(Execute(_connection, request) with
                {
                    LastInsertRowId = raw.sqlite3_last_insert_rowid(_connection.Handle!),
                });
                RecordTextualWrites(request.CommandText, raw.sqlite3_total_changes(_connection.Handle!) - changesBefore);
            }
        }
        finally
        {
            // Also on failure: the statements before the one that failed have run, and in
            // autocommit mode they have committed.
            NotifyIfCommitted(_connection);
        }

        return Task.FromResult<IReadOnlyList<BsqliteCommandResult>>(results);
    }

    /// <inheritdoc />
    public Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("The transport is not open.");
        }

        var pointer = raw.sqlite3_serialize(_connection.Handle!, "main", out var size, 0);
        if (pointer == IntPtr.Zero)
        {
            if (size == 0)
            {
                return Task.FromResult(Array.Empty<byte>());
            }

            throw new BsqliteException("sqlite3_serialize could not snapshot the database.");
        }

        try
        {
            var image = new byte[size];
            Marshal.Copy(pointer, image, 0, image.Length);
            return Task.FromResult(image);
        }
        finally
        {
            raw.sqlite3_free(pointer);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// A semaphore per database and name, shared by every transport in the process that opens the
    /// same file or shared-cache memory database - the desktop stand-in for a Web Lock.
    /// </remarks>
    public async Task<IAsyncDisposable> AcquireLockAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var gate = _locks.GetOrAdd($"{_lockScope}|{name}", _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new HeldLock(gate);
    }

    /// <summary>
    /// Adds the tables a request's SQL names - the same rule as the worker's
    /// <c>recordTextualWrites</c>.
    /// </summary>
    /// <remarks>
    /// The update hook is exact for row changes but blind to DDL and to the truncate-optimised
    /// <c>DELETE FROM t</c>, so the statement text fills those in - only when the request changed
    /// rows or reads as DDL, so a write that matched nothing raises nothing.
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
    /// Raises <see cref="TablesChanged"/> for what was written, once it is committed - the same
    /// rule as the worker's <c>notifyIfCommitted</c>.
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

        if (committed && tables.Count > 0)
        {
            TablesChanged?.Invoke(this, new BsqliteTablesChangedEventArgs(tables));
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
        // Computed columns have no declared type and Microsoft.Data.Sqlite throws rather than
        // returning null.
        try
        {
            return reader.GetDataTypeName(ordinal);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        _connection = null;
        return ValueTask.CompletedTask;
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
