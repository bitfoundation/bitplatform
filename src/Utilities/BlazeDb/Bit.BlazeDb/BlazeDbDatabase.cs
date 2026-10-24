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
    private readonly BlazeDbTable<string, BlazeDbMetaEntry> _meta;

    // Committed changes waiting to be handed to Changed subscribers once the write lock is released.
    // The queue is guarded by SyncRoot; the count is read without it, so a write with nothing to
    // deliver skips the delivery path with one field read.
    private readonly Queue<BlazeDbChangeCollector> _pendingChanges = new();
    private volatile int _pendingChangeCount;
    private readonly object _deliveryLock = new();
    private BlazeDbTransaction? _activeTransaction;
    private BlazeDbWalManager? _wal;
    private long _nextSeq;
    private volatile bool _disposed;
    private int _disposeState;

    internal readonly object SyncRoot = new();

    private BlazeDbDatabase(BlazeDbDatabaseOptions options)
    {
        _meta = (BlazeDbTable<string, BlazeDbMetaEntry>)BlazeDbMetaTable.Descriptor.CreateTable(this);
        _tablesByName.Add(_meta.Name, _meta);
        _tablesByDescriptor.Add(BlazeDbMetaTable.Descriptor, _meta);
        _tableList.Add(_meta);
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
    /// a storage backend is configured, starts the background flusher, and runs the migrations
    /// (<see cref="BlazeDbDatabaseOptions.AddMigration"/>) the database has not had yet.
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
        if (!db.IsReadOnly && options.Migrations.Count > 0)
        {
            try
            {
                db.RunMigrations(options.Migrations);
            }
            catch
            {
                // What earlier migrations committed is flushed; the failed one left nothing behind.
                await db.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }
        return db;
    }

    /// <summary>
    /// Runs each migration whose version is above <see cref="UserVersion"/>, lowest first, every one in
    /// a transaction of its own that also records its version - so a migration either completes and is
    /// never run again, or fails and leaves the database at the version before it, to be retried on the
    /// next open.
    /// </summary>
    private void RunMigrations(IReadOnlyList<BlazeDbMigration> migrations)
    {
        foreach (var migration in migrations.OrderBy(m => m.Version))
        {
            if (migration.Version <= UserVersion)
            {
                continue;
            }
            RunInTransaction(() =>
            {
                migration.Migrate(this);
                SetUserVersion(migration.Version);
            });
        }
    }

    /// <summary>
    /// A number the application keeps with the data to say which shape it is in - 0 until something sets
    /// it. It is what <see cref="BlazeDbDatabaseOptions.AddMigration"/> compares against, the way SQLite's
    /// <c>user_version</c> or IndexedDB's version is used; the engine attaches no other meaning to it. It
    /// is stored in the database itself, so it travels with exports and is read back on every open.
    /// </summary>
    public long UserVersion =>
        _meta.TryGet(BlazeDbMetaTable.UserVersionKey, out var entry) ? entry.Value : 0;

    /// <summary>
    /// Records <paramref name="version"/> as the <see cref="UserVersion"/>. It is an ordinary write: inside
    /// a transaction it commits or rolls back with the transaction's other writes, which is how a
    /// migration and the version it reaches land together.
    /// </summary>
    public void SetUserVersion(long version)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(version);
        _meta.Upsert(new BlazeDbMetaEntry(BlazeDbMetaTable.UserVersionKey, version));
    }

    /// <summary>
    /// Writes the whole database - every table's rows, those of unregistered tables and the
    /// <see cref="UserVersion"/> included - to <paramref name="destination"/> as one self-checking file, for a
    /// backup to download or a copy to move elsewhere. <see cref="ImportAsync"/> turns the file back into a
    /// database on any storage backend. The rows are captured at once, under the write lock, so the file is
    /// one consistent state; writing it out happens afterwards, without holding anything.
    /// <para>
    /// The file is a snapshot in the engine's own format, guarded by a checksum: an export that is cut
    /// short or altered is refused on import rather than read in part. It is not encrypted - wrap the
    /// stream, or keep the file somewhere the data may live, if it is sensitive.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">A transaction is open: its writes are not yet committed, so there is no consistent state to capture.</exception>
    public async ValueTask ExportAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        CheckDisposed();
        ReadOnlyMemory<byte> snapshot;
        lock (SyncRoot)
        {
            if (_activeTransaction is not null)
            {
                throw new InvalidOperationException(
                    "Cannot export while a transaction is active: its writes are not committed yet. Export once it ends.");
            }
            snapshot = BlazeDbWalManager.BuildSnapshot(_tableList, lsn: 0, sizeHint: 64 * 1024);
        }
        await destination.WriteAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a database on <paramref name="destination"/> from a file <see cref="ExportAsync"/> wrote;
    /// open it afterwards with <see cref="OpenAsync"/> as usual. The file's checksum is verified before
    /// anything is written. The destination must not hold a database already - remove one first with
    /// <see cref="DeleteAsync"/> - so an import never overwrites data by accident.
    /// </summary>
    /// <exception cref="BlazeDbCorruptDatabaseException">The file is not an export, or was truncated or altered.</exception>
    /// <exception cref="InvalidOperationException">The destination already holds a database.</exception>
    public static async ValueTask ImportAsync(Stream source, IBlazeDbStorage destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        await BlazeDbWalManager.ImportAsync(destination, buffer.GetBuffer().AsMemory(0, (int)buffer.Length), cancellationToken).ConfigureAwait(false);
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
        lock (SyncRoot)
        {
            _pendingChanges.Enqueue(new BlazeDbChangeCollector(isReset: true));
            _pendingChangeCount++;
        }
        DeliverChanges();
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
    /// while active, every table write on this database becomes part of it. A second call while one
    /// is open throws: to compose atomic units inside a transaction, use <see cref="RunInTransaction"/>
    /// or a savepoint (<see cref="BlazeDbTransaction.CreateSavepoint"/>).
    /// </summary>
    public BlazeDbTransaction BeginTransaction() => BeginTransaction(owner: null);

    /// <summary>
    /// Runs <paramref name="action"/> as one atomic unit: every write it makes is kept, or - when it
    /// throws - none is, and the exception propagates. With no transaction open, the unit commits as one
    /// transaction and one log record. Inside an open transaction it joins it behind a savepoint, so a
    /// failure takes back only the unit's own writes and the transaction stays open; nested calls nest
    /// the same way.
    /// <para>
    /// The action runs synchronously under the database's write lock, so no other thread's write can
    /// land in the middle of it. Do not block inside it on work that needs to write to this database.
    /// There is deliberately no asynchronous overload: an <c>await</c> inside a transaction lets other
    /// code run - another event handler on WebAssembly - and its writes would join the transaction.
    /// </para>
    /// </summary>
    public void RunInTransaction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        RunInTransaction(action, static action =>
        {
            action();
            return true;
        });
    }

    /// <summary>
    /// <see cref="RunInTransaction(Action)"/> for a unit that produces a result, which is returned once
    /// its writes have committed.
    /// </summary>
    public TResult RunInTransaction<TResult>(Func<TResult> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return RunInTransaction(action, static action => action());
    }

    private TResult RunInTransaction<TState, TResult>(TState state, Func<TState, TResult> body)
    {
        var result = RunInTransactionCore(state, body);
        DeliverChanges();
        return result;
    }

    private TResult RunInTransactionCore<TState, TResult>(TState state, Func<TState, TResult> body)
    {
        EnsureWritable();
        lock (SyncRoot)
        {
            if (_activeTransaction is { } outer)
            {
                var unit = outer.BeginUnit();
                TResult unitResult;
                try
                {
                    unitResult = body(state);
                }
                catch
                {
                    // The outer transaction may have been ended inside the body; then there is
                    // nothing left of it to roll back to.
                    if (ReferenceEquals(_activeTransaction, outer))
                    {
                        outer.RollbackUnit(unit);
                    }
                    throw;
                }
                if (ReferenceEquals(_activeTransaction, outer))
                {
                    outer.EndUnit(unit);
                }
                return unitResult;
            }

            using var transaction = BeginTransaction(owner: null);
            var result = body(state);
            transaction.Commit();
            return result;
        }
    }

    /// <summary>
    /// Checks every table's rows against its indexes and returns what disagrees - nothing, on a healthy
    /// database. The engine keeps them in step itself, so an issue means a row object was changed in
    /// place after it was written (its key, or a property an index covers), which leaves lookups
    /// returning rows that no longer match them. It reads every row and every index entry, so it is a
    /// diagnostic for tests and support tools rather than something to run on every render.
    /// </summary>
    public IReadOnlyList<BlazeDbIntegrityIssue> VerifyIntegrity()
    {
        CheckDisposed();
        var issues = new List<BlazeDbIntegrityIssue>();
        lock (SyncRoot)
        {
            foreach (var table in _tableList)
            {
                table.VerifyIntegrity(issues);
            }
        }
        return issues;
    }

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

    /// <summary>
    /// Closes the database: a transaction still open is rolled back - it can no longer be committed,
    /// and memory should describe what storage will hold - and everything committed is flushed before
    /// the background flusher stops. The database does not own its storage; dispose that afterwards.
    /// Calling it again does nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }
        Exception? revertFailure = null;
        lock (SyncRoot)
        {
            _disposed = true;
            if (_activeTransaction is { } open)
            {
                try
                {
                    RevertOps(open.Ops);
                }
                catch (Exception ex)
                {
                    // Reported once the log is closed: whatever the undo left behind was never
                    // journaled, and stopping the flusher must not depend on it.
                    revertFailure = ex;
                    Poison(ex);
                }
                finally
                {
                    open.MarkCompleted();
                    _activeTransaction = null;
                    ActiveTransactionOwner = null;
                }
            }
        }
        if (_wal is not null)
        {
            await _wal.DisposeAsync().ConfigureAwait(false);
        }
        if (revertFailure is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(revertFailure);
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
                return;
            }
            if (_wal is not null)
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
                    RevertOrPoison(op.Revert);
                    throw;
                }
                finally
                {
                    _singleOpScratch[0] = null!;
                }
            }
            if ((Changed is not null && !op.IsEngineTable) || op.IsObserved)
            {
                var collector = new BlazeDbChangeCollector();
                op.CollectChange(collector);
                QueueChanges(collector);
            }
        }
    }

    // The checks that a transaction is still the active one run under the lock, together with the
    // work they guard: disposing the database rolls back the transaction it finds open, and a commit
    // or rollback racing it on another thread must find it gone rather than undo its writes twice.

    internal void CommitTransaction(BlazeDbTransaction transaction)
    {
        lock (SyncRoot)
        {
            VerifyActive(transaction);
            try
            {
                if (transaction.Ops.Count > 0)
                {
                    _wal?.AppendCommit(transaction.Ops);
                    CollectChanges(transaction.Ops);
                }
            }
            catch
            {
                // Memory must never run ahead of what the log will replay, exactly as for an
                // autocommit write: a batch that could not be journaled is undone here and now,
                // and the transaction ends, rather than staying applied until someone disposes it.
                RevertOrPoison(() => RevertOps(transaction.Ops));
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
        lock (SyncRoot)
        {
            VerifyActive(transaction);
            try
            {
                RevertOps(transaction.Ops);
            }
            catch (Exception ex)
            {
                Poison(ex);
                throw;
            }
            finally
            {
                // Even if undoing a write threw, the transaction is over: left registered, it would
                // swallow every later write on the database into a batch nobody can commit.
                _activeTransaction = null;
                ActiveTransactionOwner = null;
            }
        }
    }

    internal void RollbackTransactionTo(BlazeDbTransaction transaction, int savepoint)
    {
        lock (SyncRoot)
        {
            VerifyActive(transaction);
            var ops = transaction.Ops;
            if ((uint)savepoint > (uint)ops.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(savepoint));
            }
            // One write at a time, forgetting each as soon as it is undone: if undoing one throws, the
            // transaction still lists exactly the writes memory still holds, so a later commit
            // journals what is there and a later rollback does not undo anything twice.
            for (var i = ops.Count - 1; i >= savepoint; i--)
            {
                try
                {
                    ops[i].Revert();
                }
                catch (Exception ex)
                {
                    Poison(ex);
                    throw;
                }
                transaction.Truncate(i);
            }
        }
    }

    // ---- Change notifications ----

    /// <summary>
    /// Raised after each commit with the net change to every row it touched, across all tables. The
    /// rules are those of <see cref="BlazeDbTable{TKey, TRow}.Changed"/>, which is raised first, table by
    /// table. Rows and keys arrive as objects: this is the event for code that follows the whole
    /// database - an audit log, a sync outbox, a "saved" indicator - rather than one table.
    /// </summary>
    public event EventHandler<BlazeDbChangedEventArgs>? Changed;

    internal bool HasChangedSubscribers => Changed is not null;

    internal void RaiseChanged(BlazeDbChangedEventArgs args, ref List<Exception>? errors) =>
        BlazeDbChangeCollector.Raise(Changed, this, args, ref errors);

    /// <summary>Collects a committed transaction's changes, when anything is listening. Called under <see cref="SyncRoot"/>.</summary>
    private void CollectChanges(IReadOnlyList<IBlazeDbTxnOp> ops)
    {
        var all = Changed is not null;
        BlazeDbChangeCollector? collector = null;
        foreach (var op in ops)
        {
            if ((all && !op.IsEngineTable) || op.IsObserved)
            {
                op.CollectChange(collector ??= new BlazeDbChangeCollector());
            }
        }
        if (collector is not null)
        {
            QueueChanges(collector);
        }
    }

    private void QueueChanges(BlazeDbChangeCollector collector)
    {
        _pendingChanges.Enqueue(collector);
        _pendingChangeCount++;
    }

    /// <summary>
    /// Hands queued changes to their subscribers, in commit order. Every public write calls it once its
    /// lock is released; one that still holds the lock - a write inside <see cref="RunInTransaction"/> -
    /// or that is itself running inside a handler leaves the queue to the outer call, so handlers never
    /// run under the write lock and one commit's notification reaches every subscriber before the next
    /// one starts. The exceptions handlers threw are rethrown together once everything was delivered:
    /// the commits they concern have happened, and say so.
    /// </summary>
    internal void DeliverChanges()
    {
        if (_pendingChangeCount == 0 || Monitor.IsEntered(SyncRoot) || Monitor.IsEntered(_deliveryLock))
        {
            return;
        }
        List<Exception>? errors = null;
        lock (_deliveryLock)
        {
            while (true)
            {
                BlazeDbChangeCollector next;
                IBlazeDbTableInternal[] tables;
                lock (SyncRoot)
                {
                    if (_pendingChanges.Count == 0)
                    {
                        break;
                    }
                    next = _pendingChanges.Dequeue();
                    _pendingChangeCount--;
                    tables = next.IsReset ? _tableList.ToArray() : [];
                }
                next.Deliver(this, tables, ref errors);
            }
        }
        if (errors is not null)
        {
            throw new BlazeDbChangeHandlerException(errors.Count == 1 ? errors[0] : new AggregateException(errors));
        }
    }

    /// <summary>
    /// Set when undoing a write failed - only an index whose selector or comparer throws can do that. Memory
    /// then holds a write the log never recorded, and a checkpoint would make it durable, so from then on
    /// the database refuses writes and checkpoints and says why; reads go on. Reopening rebuilds memory
    /// from storage, which never saw the write.
    /// </summary>
    private volatile Exception? _undoFailure;

    /// <summary>Whether memory may hold an unjournaled write, after an undo failed. Checkpoints refuse then.</summary>
    internal Exception? UndoFailure => _undoFailure;

    private void Poison(Exception cause) => _undoFailure ??= cause;

    /// <summary>Runs an undo; if it fails, records that and keeps the exception being handled as the one that propagates.</summary>
    private void RevertOrPoison(Action revert)
    {
        try
        {
            revert();
        }
        catch (Exception ex)
        {
            Poison(ex);
        }
    }

    private static void RevertOps(IReadOnlyList<IBlazeDbTxnOp> ops)
    {
        for (var i = ops.Count - 1; i >= 0; i--)
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
        if (_undoFailure is { } cause)
        {
            throw new BlazeDbException(
                "An earlier rollback could not undo a write - an index selector or comparer threw - so memory may " +
                "hold a write the log never recorded. Writes are refused until the database is reopened from " +
                "storage. See the inner exception for the original failure.", cause);
        }
    }
}
