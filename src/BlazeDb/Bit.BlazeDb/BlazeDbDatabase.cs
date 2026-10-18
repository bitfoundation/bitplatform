using Bit.BlazeDb.Storage;
using Bit.BlazeDb.Wal;

namespace Bit.BlazeDb;

/// <summary>
/// A memory-first database instance. All live data resides in managed memory; reads never touch
/// storage. Writes are applied to memory synchronously and (when a storage backend is
/// configured) appended to a write-ahead log flushed in the background.
/// The engine assumes a single logical writer and no concurrent readers (Blazor WASM's
/// single-threaded model); mutations and snapshot serialization are briefly locked so the
/// background flusher stays consistent, but reads take no lock and must not overlap a write on
/// another thread. A multi-threaded host has to serialize all access itself.
/// </summary>
public sealed class BlazeDbDatabase : IAsyncDisposable
{
    private readonly Dictionary<string, IBlazeDbTableInternal> _tablesByName = new();
    private readonly Dictionary<BlazeDbTableDescriptor, IBlazeDbTableInternal> _tablesByDescriptor = new();
    private readonly List<IBlazeDbTableInternal> _tableList = [];
    private readonly IBlazeDbTxnOp[] _singleOpScratch = new IBlazeDbTxnOp[1];
    private BlazeDbTransaction? _activeTransaction;
    private BlazeDbWalManager? _wal;
    private long _nextSeq;
    private bool _disposed;

    internal readonly object SyncRoot = new();

    private BlazeDbDatabase(BlazeDbDatabaseOptions options)
    {
        foreach (var descriptor in options.Tables)
        {
            var table = descriptor.CreateTable(this);
            if (!_tablesByName.TryAdd(table.Name, table))
            {
                throw new BlazeDbException($"Duplicate table name '{table.Name}'.");
            }
            _tablesByDescriptor.Add(descriptor, table);
            _tableList.Add(table);
        }
    }

    /// <summary>
    /// Opens a database: creates tables, recovers persisted state (snapshot + WAL replay) when
    /// a storage backend is configured, and starts the background flusher.
    /// </summary>
    public static async ValueTask<BlazeDbDatabase> OpenAsync(BlazeDbDatabaseOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        var db = new BlazeDbDatabase(options) { IsReadOnly = options.ReadOnly };
        if (options.Storage is not null)
        {
            db._wal = await BlazeDbWalManager.OpenAsync(db, options, cancellationToken).ConfigureAwait(false);
        }
        return db;
    }

    /// <summary>
    /// Removes a database from a storage backend. Every file when the backend can list them (see
    /// <see cref="IBlazeDbEnumerableStorage"/>), otherwise the manifest together with the snapshot
    /// and log it names. No database may be open on the backend while this runs.
    /// </summary>
    public static ValueTask DeleteAsync(IBlazeDbStorage storage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        return BlazeDbWalManager.DeleteAsync(storage, cancellationToken);
    }

    /// <summary>True when this instance is a read-only replica and refuses all mutations.</summary>
    public bool IsReadOnly { get; private init; }

    /// <summary>
    /// Discards the in-memory state and rebuilds it from storage (snapshot plus WAL replay),
    /// picking up everything another tab has flushed. Intended for read-only replicas; the
    /// writer's own memory is already the newest copy. Existing row references are not updated -
    /// re-query after reloading.
    /// </summary>
    public async ValueTask ReloadAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        if (!IsReadOnly)
        {
            // The writer's memory is the newest state there is; rebuilding it from storage would
            // silently drop every commit the flusher has not written yet.
            throw new InvalidOperationException(
                "ReloadAsync is for read-only replicas. This database holds the writer's state, " +
                "which is already newer than anything on storage.");
        }
        if (_wal is null)
        {
            return;
        }
        if (_activeTransaction is not null)
        {
            throw new InvalidOperationException("Cannot reload while a transaction is active.");
        }
        await _wal.ReloadAsync(cancellationToken).ConfigureAwait(false);
    }

    public BlazeDbTable<TKey, TRow> GetTable<TKey, TRow>(BlazeDbTableDescriptor<TKey, TRow> descriptor)
        where TKey : notnull
    {
        if (_tablesByDescriptor.TryGetValue(descriptor, out var table))
        {
            return (BlazeDbTable<TKey, TRow>)table;
        }
        throw new BlazeDbException($"Table '{descriptor.Name}' was not registered in BlazeDbDatabaseOptions.");
    }

    /// <summary>
    /// Adds <paramref name="descriptor"/> if it is not already present. Used by the EF Core provider
    /// when a second context type shares an already-open store and brings extra entity types. If
    /// recovery carried rows for a table of this name that no descriptor claimed at the time (see
    /// <see cref="BlazeDbOpaqueTable"/>), they are decoded into the new table here.
    /// </summary>
    internal void EnsureTable(BlazeDbTableDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        CheckDisposed();
        lock (SyncRoot)
        {
            if (_tablesByDescriptor.ContainsKey(descriptor))
            {
                return;
            }

            var table = descriptor.CreateTable(this);
            if (_tablesByName.TryGetValue(table.Name, out var existing))
            {
                if (existing is not BlazeDbOpaqueTable opaque)
                {
                    throw new BlazeDbException($"Duplicate table name '{table.Name}'.");
                }
                foreach (var (key, row) in opaque.Rows)
                {
                    table.ReplaySet(key, row);
                }
                _tablesByName[table.Name] = table;
                _tableList[_tableList.IndexOf(opaque)] = table;
            }
            else
            {
                _tablesByName.Add(table.Name, table);
                _tableList.Add(table);
            }
            _tablesByDescriptor.Add(descriptor, table);
        }
    }

    /// <summary>
    /// Starts an explicit transaction. Only one can be active at a time (single-writer model);
    /// while active, every table write on this database becomes part of it.
    /// </summary>
    public BlazeDbTransaction BeginTransaction() => BeginTransaction(owner: null);

    /// <summary>
    /// Starts a transaction and records who it belongs to. The EF Core provider passes its own
    /// transaction manager, so a save made by a different context on the same store can tell that the
    /// open transaction is not one it may join and commit into.
    /// </summary>
    internal BlazeDbTransaction BeginTransaction(object? owner)
    {
        CheckDisposed();
        CheckWritable();
        // Under the lock so a transaction cannot open around a write another thread is part-way
        // through, which would pull that write into it - and roll it back with it.
        lock (SyncRoot)
        {
            if (_activeTransaction is not null)
            {
                throw new InvalidOperationException("A transaction is already active; nested transactions are not supported.");
            }
            ActiveTransactionOwner = owner;
            return _activeTransaction = new BlazeDbTransaction(this);
        }
    }

    /// <summary>
    /// Forces all buffered WAL commits to storage now (hard durability point). Without an
    /// explicit flush, durability lags commits by at most <see cref="BlazeDbDatabaseOptions.FlushInterval"/>.
    /// </summary>
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        return _wal?.FlushAsync(cancellationToken) ?? default;
    }

    /// <summary>
    /// The error the most recent background flush or checkpoint failed with, or null when the last
    /// one succeeded (or no storage is configured). Committed data is safe in memory and the flusher
    /// keeps retrying; this exists so an application can show that durability is lagging - for
    /// example after a quota refusal - without waiting for an explicit <see cref="FlushAsync"/>.
    /// </summary>
    public Exception? LastBackgroundError => _wal?.LastBackgroundError;

    /// <summary>
    /// How much of the origin's storage allowance is in use, or null when the backend cannot
    /// report it (everything except the browser backends). Useful for showing headroom in a UI
    /// before the engine has to refuse a flush.
    /// </summary>
    public ValueTask<BlazeDbStorageQuota?> GetStorageQuotaAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        return _wal?.GetQuotaAsync(cancellationToken) ?? default;
    }

    /// <summary>Writes a full snapshot and truncates the WAL (compaction).</summary>
    public async ValueTask CheckpointAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        if (_wal is not null)
        {
            await _wal.CheckpointAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        if (_wal is not null)
        {
            await _wal.DisposeAsync().ConfigureAwait(false);
        }
    }

    // ---- Internals used by tables, transactions and the WAL manager ----

    internal bool HasActiveTransaction => _activeTransaction is not null;

    /// <summary>The open transaction every write currently joins, or null.</summary>
    internal BlazeDbTransaction? ActiveTransaction => _activeTransaction;

    /// <summary>What was passed to <see cref="BeginTransaction(object?)"/>, or null for one the application opened itself.</summary>
    internal object? ActiveTransactionOwner { get; private set; }

    internal List<IBlazeDbTableInternal> TableList => _tableList;

    /// <summary>
    /// The table persisted data refers to. A name no descriptor claims is not an error: the rows are
    /// carried as raw bytes (<see cref="BlazeDbOpaqueTable"/>) so a database opened with part of its
    /// model - or after a table type was removed - still opens, and every checkpoint writes those rows
    /// back out. Called under <see cref="SyncRoot"/> by recovery and replay.
    /// </summary>
    internal IBlazeDbTableInternal GetTableByName(string name)
    {
        if (!_tablesByName.TryGetValue(name, out var table))
        {
            table = new BlazeDbOpaqueTable(name);
            _tablesByName.Add(name, table);
            _tableList.Add(table);
        }
        return table;
    }

    /// <summary>
    /// Names of tables the persisted data holds rows for but no registered descriptor describes.
    /// Their rows are preserved across checkpoints but cannot be queried until a descriptor for
    /// them is registered on a later open.
    /// </summary>
    public IReadOnlyList<string> UnregisteredTables
    {
        get
        {
            lock (SyncRoot)
            {
                return _tableList.OfType<BlazeDbOpaqueTable>().Select(t => t.Name).ToArray();
            }
        }
    }

    internal long NextSeq() => ++_nextSeq;

    /// <summary>Clears every table, for a replica about to rebuild from a newer snapshot.</summary>
    internal void ResetTables()
    {
        lock (SyncRoot)
        {
            foreach (var table in _tableList)
            {
                table.Clear();
            }
            _nextSeq = 0;
        }
    }

    /// <summary>
    /// The pre-flight checks every write makes, exposed so a table can run them before it touches
    /// its own state on the paths that have to adjust the row map ahead of the write itself.
    /// </summary>
    internal void EnsureWritable()
    {
        CheckDisposed();
        CheckWritable();
    }

    internal void ApplyWrite(IBlazeDbTxnOp op)
    {
        EnsureWritable();
        lock (SyncRoot)
        {
            op.Apply();
            if (_activeTransaction is not null)
            {
                _activeTransaction.Record(op);
            }
            else if (_wal is not null)
            {
                _singleOpScratch[0] = op;
                try
                {
                    _wal.AppendCommit(_singleOpScratch);
                }
                catch
                {
                    // Memory must never run ahead of what the log will replay: an autocommit
                    // write that could not be journaled is undone rather than left in place.
                    op.Revert();
                    throw;
                }
                finally
                {
                    _singleOpScratch[0] = null!;
                }
            }
        }
    }

    internal void CommitTransaction(BlazeDbTransaction transaction)
    {
        VerifyActive(transaction);
        lock (SyncRoot)
        {
            try
            {
                if (transaction.Ops.Count > 0)
                {
                    _wal?.AppendCommit(transaction.Ops);
                }
            }
            catch
            {
                // Memory must never run ahead of what the log will replay, exactly as for an
                // autocommit write: a batch that could not be journaled is undone here and now,
                // and the transaction ends, rather than staying applied until someone disposes it.
                RevertOps(transaction.Ops);
                throw;
            }
            finally
            {
                _activeTransaction = null;
                ActiveTransactionOwner = null;
            }
        }
    }

    internal void RollbackTransaction(BlazeDbTransaction transaction)
    {
        VerifyActive(transaction);
        lock (SyncRoot)
        {
            RevertOps(transaction.Ops);
            _activeTransaction = null;
            ActiveTransactionOwner = null;
        }
    }

    internal void RollbackTransactionTo(BlazeDbTransaction transaction, int savepoint)
    {
        VerifyActive(transaction);
        lock (SyncRoot)
        {
            var ops = transaction.Ops;
            if ((uint)savepoint > (uint)ops.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(savepoint));
            }
            RevertOps(ops, savepoint);
            transaction.Truncate(savepoint);
        }
    }

    private static void RevertOps(IReadOnlyList<IBlazeDbTxnOp> ops, int downTo = 0)
    {
        for (var i = ops.Count - 1; i >= downTo; i--)
        {
            ops[i].Revert();
        }
    }

    private void VerifyActive(BlazeDbTransaction transaction)
    {
        if (!ReferenceEquals(_activeTransaction, transaction))
        {
            throw new InvalidOperationException("BlazeDbTransaction is not the active transaction of this database.");
        }
    }

    private void CheckDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private void CheckWritable()
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException(
                "This database was opened as a read-only replica. Writes belong to the tab holding " +
                "the writer lock; call ReloadAsync to pick up its changes.");
        }
    }
}
