using System.Runtime.CompilerServices;
using System.Text;

namespace Bit.Bcrdt.BlazeDb;

/// <summary>Settings for a <see cref="BcrdtBlazeDbDocumentStore"/>.</summary>
public sealed class BcrdtBlazeDbDocumentStoreOptions
{
    /// <summary>Whether a write completes only once the database has flushed it to its storage.</summary>
    /// <remarks>
    /// <para>
    /// On by default, so an update the store has accepted survives a crash, as it does in every store that
    /// ships with bit Bcrdt. A BlazeDb commit lands in memory at once and reaches storage on the database's
    /// background flusher, within its <c>FlushInterval</c>; with this on, the store waits for it.
    /// </para>
    /// <para>
    /// Turn it off to make an append cost one in-memory commit and leave durability to the flusher: a crash
    /// then loses at most what was committed in the last flush interval, which a peer or server still holds
    /// and resends. A database without storage has nothing to flush, and the setting changes nothing there.
    /// </para>
    /// </remarks>
    public bool WaitForDurability { get; init; } = true;

    /// <summary>The lock the store takes around each of its operations on the database.</summary>
    /// <remarks>
    /// <para>
    /// BlazeDb assumes a single logical writer and no concurrent readers - which Blazor WebAssembly, with its
    /// one thread, gives by construction - and a multi-threaded host has to serialize every access itself.
    /// The store's writes run on a background loop, so on such a host (Blazor Server, MAUI, a desktop app)
    /// pass the lock the application already takes around its own reads and writes of the database here,
    /// and the store takes it too.
    /// </para>
    /// <para>
    /// Left unset, the store uses a lock of its own, shared by every store on the same database: the stores
    /// are serialized with each other, but not with the application.
    /// </para>
    /// </remarks>
    public SemaphoreSlim? Gate { get; init; }
}

/// <summary>
/// A store that keeps documents in a bit BlazeDb database - the application's own, so its collaborative
/// documents live beside the rest of its data and persist wherever the database does: to OPFS or IndexedDB in
/// Blazor WebAssembly, to files on the server, the desktop and MAUI.
/// </summary>
/// <remarks>
/// <para>
/// Every update is one row of a table the application registers when it opens its database
/// (<c>options.AddTable(BcrdtBlazeDbDocumentStore.Table)</c>), keyed by a sequence number and indexed by
/// document. An append is one commit of one row - in memory at once, and in BlazeDb's write-ahead log on its
/// next flush - so it stays cheap however large the document grows.
/// </para>
/// <para>
/// A torn or damaged record needs no framing of its own here: BlazeDb checksums every log record, cuts off a
/// torn tail when it recovers, and comes back as of a commit boundary, so a crash costs at most the commits
/// that had not been flushed - with <see cref="BcrdtBlazeDbDocumentStoreOptions.WaitForDurability"/> on, at most
/// the update being written. Damage anywhere else in its files is reported by BlazeDb itself, which refuses to
/// open the database rather than cut away what follows it. What this store guards against is a row that is
/// intact but is not an update this build can use - written by something else, or by a newer build: it is
/// skipped, reported through <see cref="UnreadableRecordSkipped"/> and left where it is, so the rest of the
/// document still opens.
/// </para>
/// <para>
/// Compaction reads a document's records, merges them and then, in one BlazeDb transaction, deletes exactly the
/// records it merged and inserts the result. A transaction commits as one log record, so a crash leaves either
/// every original record or the merged one, never both halves and never neither. Every write of the store is a
/// transaction of its own: one the application has open is never joined, so rolling it back cannot take an
/// update the store reported stored with it.
/// </para>
/// <para>
/// Document identifiers are stored as values and looked up through an index, never turned into names of any
/// kind, so an identifier taken from a URL needs no sanitizing. One that is not well-formed Unicode - a lone
/// surrogate - is refused, because it could not be read back as the same string.
/// </para>
/// <para>
/// BlazeDb keeps every row in memory for as long as the database is open, so every stored document's bytes
/// stay resident whether or not it is open - compaction keeps that to one merged update per document. A
/// read-only replica of the database - a second tab, under BlazeDb's single-writer model - loads documents but
/// refuses every write with <see cref="NotSupportedException"/>; give each tab its own database, or use
/// <see cref="BcrdtIndexedDbDocumentStore"/>, when several tabs must each persist their own edits.
/// </para>
/// </remarks>
public sealed class BcrdtBlazeDbDocumentStore : IBcrdtDocumentStore
{
    /// <summary>The name of the table the store keeps its records in.</summary>
    public const string TableName = "bit-bcrdt-updates";

    /// <summary>
    /// The largest update stored as one record. A merged document larger than this is left uncompacted rather
    /// than written as one commit too large for BlazeDb's log.
    /// </summary>
    private const int MaxRecordSize = 64 * 1024 * 1024;

    private static readonly ConditionalWeakTable<BlazeDbDatabase, DatabaseState> States = new();

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly Func<CancellationToken, ValueTask<BlazeDbDatabase>> _open;
    private readonly BcrdtBlazeDbDocumentStoreOptions _options;
    private readonly SemaphoreSlim _openGate = new(1, 1);
    private BlazeDbDatabase? _database;

    /// <summary>Creates a store over an open database.</summary>
    /// <param name="database">
    /// The database to keep documents in. It must have <see cref="Table"/> registered; the store does not own it
    /// and never disposes it.
    /// </param>
    /// <param name="options">How writes are made durable and serialized.</param>
    public BcrdtBlazeDbDocumentStore(BlazeDbDatabase database, BcrdtBlazeDbDocumentStoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(database);

        _database = database;
        _open = _ => ValueTask.FromResult(database);
        _options = options ?? new BcrdtBlazeDbDocumentStoreOptions();
    }

    /// <summary>Creates a store over a database that is opened when the store is first used.</summary>
    /// <remarks>
    /// For the usual WebAssembly shape, where the database is opened asynchronously - on OPFS, behind the tab's
    /// writer lock - by a service the application shares: pass that service's <c>GetAsync</c>. A failed open is
    /// not remembered; the next operation asks again.
    /// </remarks>
    /// <param name="database">
    /// Returns the database to keep documents in, once. It must have <see cref="Table"/> registered; the store
    /// does not own it and never disposes it.
    /// </param>
    /// <param name="options">How writes are made durable and serialized.</param>
    public BcrdtBlazeDbDocumentStore(Func<CancellationToken, ValueTask<BlazeDbDatabase>> database, BcrdtBlazeDbDocumentStoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(database);

        _open = database;
        _options = options ?? new BcrdtBlazeDbDocumentStoreOptions();
    }

    /// <summary>
    /// The table the store keeps its records in. Register it where the database is opened:
    /// <c>new BlazeDbDatabaseOptions { ... }.AddTable(BcrdtBlazeDbDocumentStore.Table)</c>.
    /// </summary>
    public static BlazeDbTableDescriptor Table => BcrdtBlazeDbUpdateRecord.Table;

    /// <summary>
    /// Raised when a stored record cannot be used - it is not an update this build reads, or it cannot be merged
    /// with the records before it. The record is left where it is and skipped, so the rest of the document still
    /// opens. Receives the document's identifier.
    /// </summary>
    public event Action<string>? UnreadableRecordSkipped;

    /// <summary>
    /// Raised when one of the application's own <c>Changed</c> handlers threw for a write the store made.
    /// </summary>
    /// <remarks>
    /// The write committed before the handlers ran, so the store reports it stored: failing it would only have
    /// it written again, and the handler throw again, for as long as the application kept running.
    /// </remarks>
    public event Action<BlazeDbChangeHandlerException>? ChangeHandlerFailed;

    /// <inheritdoc/>
    public async ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
    {
        Validate(documentId);

        List<BcrdtBlazeDbUpdateRecord> records;
        (BlazeDbDatabase database, _, SemaphoreSlim gate) = await EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            records = Records(TableOf(database), documentId);
        }
        finally
        {
            gate.Release();
        }

        (List<BcrdtBlazeDbUpdateRecord> kept, byte[]? merged) = Select(documentId, records);
        return merged is null ? default : new BcrdtStoredDocument(merged, kept.Count);
    }

    /// <inheritdoc/>
    public async ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default)
    {
        Validate(documentId);
        if (update.Length == 0) return;
        if (update.Length > MaxRecordSize)
        {
            throw new ArgumentException($"Update of {update.Length} bytes exceeds the {MaxRecordSize} byte record limit.", nameof(update));
        }

        byte[] record = update.ToArray();
        (BlazeDbDatabase database, DatabaseState state, SemaphoreSlim gate) = await EnterAsync(cancellationToken).ConfigureAwait(false);
        BlazeDbChangeHandlerException? handlerFailure;
        try
        {
            RequireWritable(database);

            // A record committed by an earlier append whose flush failed is still in the database's memory, and
            // the update it held is the one being retried now. Committing it again while storage keeps failing
            // would add a copy on every retry; confirm the flush first and commit only once it goes through.
            if (_options.WaitForDurability && state.Unflushed) await FlushAsync(database, state, cancellationToken).ConfigureAwait(false);

            BlazeDbTable<long, BcrdtBlazeDbUpdateRecord> table = TableOf(database);
            handlerFailure = Commit(database, () =>
            {
                // A row the application wrote - an imported backup, say - can hold the next number already.
                if (!table.TryInsert(new BcrdtBlazeDbUpdateRecord { Id = state.NextId(table), DocumentId = documentId, Update = record }))
                {
                    state.Reset();
                    table.Insert(new BcrdtBlazeDbUpdateRecord { Id = state.NextId(table), DocumentId = documentId, Update = record });
                }
            });
        }
        finally
        {
            gate.Release();
        }

        await FinishAsync(database, state, handlerFailure, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default)
    {
        Validate(documentId);

        (BlazeDbDatabase database, DatabaseState state, SemaphoreSlim gate) = await EnterAsync(cancellationToken).ConfigureAwait(false);
        BlazeDbChangeHandlerException? handlerFailure;
        try
        {
            RequireWritable(database);

            BlazeDbTable<long, BcrdtBlazeDbUpdateRecord> table = TableOf(database);
            (List<BcrdtBlazeDbUpdateRecord> kept, byte[]? merged) = Select(documentId, Records(table, documentId));

            // Several smaller records are better than one the log refuses. A record that could not be merged is
            // not among those kept, and stays exactly where it is.
            if (merged is null || kept.Count <= 1 || merged.Length > MaxRecordSize) return;

            // The merged record takes the place of the first one it replaces, so it stays ahead of any record it
            // could not merge - which is then measured against it, rather than the other way round, on every load.
            long first = kept.Min(record => record.Id);
            handlerFailure = Commit(database, () =>
            {
                table.DeleteMany([.. kept.Select(record => record.Id)]);
                table.Insert(new BcrdtBlazeDbUpdateRecord { Id = first, DocumentId = documentId, Update = merged });
            });
        }
        finally
        {
            gate.Release();
        }

        await FinishAsync(database, state, handlerFailure, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        Validate(documentId);

        (BlazeDbDatabase database, DatabaseState state, SemaphoreSlim gate) = await EnterAsync(cancellationToken).ConfigureAwait(false);
        BlazeDbChangeHandlerException? handlerFailure;
        try
        {
            BlazeDbTable<long, BcrdtBlazeDbUpdateRecord> table = TableOf(database);
            long[] ids = [.. table.Lookup(BcrdtBlazeDbUpdateRecord.Indexes.DocumentId, documentId).Select(record => record.Id)];
            if (ids.Length == 0) return;

            RequireWritable(database);
            handlerFailure = Commit(database, () => table.DeleteMany(ids));
        }
        finally
        {
            gate.Release();
        }

        await FinishAsync(database, state, handlerFailure, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        (BlazeDbDatabase database, _, SemaphoreSlim gate) = await EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return [.. TableOf(database).CountBy(BcrdtBlazeDbUpdateRecord.Indexes.DocumentId).Select(entry => entry.Key)];
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Resolves the database and takes the lock its operations are serialized with.</summary>
    private async ValueTask<(BlazeDbDatabase Database, DatabaseState State, SemaphoreSlim Gate)> EnterAsync(CancellationToken cancellationToken)
    {
        BlazeDbDatabase database = await DatabaseAsync(cancellationToken).ConfigureAwait(false);
        DatabaseState state = States.GetValue(database, static _ => new DatabaseState());
        SemaphoreSlim gate = _options.Gate ?? state.Gate;
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return (database, state, gate);
    }

    private async ValueTask<BlazeDbDatabase> DatabaseAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _database) is { } database) return database;

        await _openGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_database is { } opened) return opened;

            BlazeDbDatabase created = await _open(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The database factory given to BcrdtBlazeDbDocumentStore returned null.");
            Volatile.Write(ref _database, created);
            return created;
        }
        finally
        {
            _openGate.Release();
        }
    }

    private static BlazeDbTable<long, BcrdtBlazeDbUpdateRecord> TableOf(BlazeDbDatabase database)
    {
        try
        {
            return database.GetTable(BcrdtBlazeDbUpdateRecord.Table);
        }
        catch (BlazeDbException ex)
        {
            throw new InvalidOperationException(
                $"The database has no '{TableName}' table for bit Bcrdt's documents. Register it where the database is opened: " +
                "new BlazeDbDatabaseOptions { ... }.AddTable(BcrdtBlazeDbDocumentStore.Table).", ex);
        }
    }

    /// <summary>A document's records, in the order they were written.</summary>
    private static List<BcrdtBlazeDbUpdateRecord> Records(BlazeDbTable<long, BcrdtBlazeDbUpdateRecord> table, string documentId) =>
        [.. table.Lookup(BcrdtBlazeDbUpdateRecord.Indexes.DocumentId, documentId).OrderBy(record => record.Id)];

    /// <summary>Picks the records that make up the document and merges them.</summary>
    /// <remarks>
    /// A record that is not an update is skipped. So is one that is an update but cannot be merged with the
    /// records before it - one that leaves a hole in its author's clock range wider than any replica writes,
    /// which only a crafted update produces - since merging it in would make every load of the document fail.
    /// Finding such a record means merging the rest one at a time, so it is done only once merging all of them
    /// at once has failed.
    /// </remarks>
    /// <returns>The records kept, and their merge - a copy, for a single record - or <see langword="null"/> if none is left.</returns>
    private (List<BcrdtBlazeDbUpdateRecord> Kept, byte[]? Merged) Select(string documentId, List<BcrdtBlazeDbUpdateRecord> records)
    {
        List<BcrdtBlazeDbUpdateRecord> kept = new(records.Count);
        foreach (BcrdtBlazeDbUpdateRecord record in records)
        {
            if (IsDecodable(record.Update)) kept.Add(record);
            else UnreadableRecordSkipped?.Invoke(documentId);
        }

        if (kept.Count == 0) return (kept, null);
        if (kept.Count == 1) return (kept, (byte[])kept[0].Update.Clone());

        try
        {
            return (kept, BcrdtUpdateOperations.Merge([.. kept.Select(record => (ReadOnlyMemory<byte>)record.Update)]));
        }
        catch (BcrdtFormatException)
        {
            // Some record does not merge with the ones before it; find it, and any other.
        }

        // Merged one at a time from the oldest record, the run keeps whatever merges with it. If the oldest record is
        // itself the one that merges with nothing, that run keeps it alone; a run started from a record it refused
        // then keeps the rest. A few such restarts at most: the bad records are a crafted handful, not the history.
        (List<BcrdtBlazeDbUpdateRecord> Records, byte[] Merged) best = MergeFrom(kept, 0);
        for (int attempt = 0; attempt < MaxMergeRestarts && best.Records.Count * 2 <= kept.Count; attempt++)
        {
            int start = kept.FindIndex(record => !best.Records.Contains(record));
            if (start < 0) break;

            (List<BcrdtBlazeDbUpdateRecord> Records, byte[] Merged) run = MergeFrom(kept, start);
            if (run.Records.Count <= best.Records.Count) break;
            best = run;
        }

        for (int skipped = kept.Count - best.Records.Count; skipped > 0; skipped--) UnreadableRecordSkipped?.Invoke(documentId);

        return (best.Records, best.Records.Count == 1 ? (byte[])best.Merged.Clone() : best.Merged);
    }

    /// <summary>How many times <see cref="Select"/> starts its record-at-a-time merge over from another record.</summary>
    private const int MaxMergeRestarts = 3;

    /// <summary>
    /// Merges the records one at a time, starting from <paramref name="start"/> and leaving out each one that does
    /// not merge with those taken so far.
    /// </summary>
    /// <returns>The records merged, in the order they were written, and their merge.</returns>
    private static (List<BcrdtBlazeDbUpdateRecord> Records, byte[] Merged) MergeFrom(List<BcrdtBlazeDbUpdateRecord> records, int start)
    {
        // Merging does not depend on the order of the updates, so the records before the start are taken after it.
        List<BcrdtBlazeDbUpdateRecord> merging = [records[start]];
        byte[] merged = records[start].Update;
        for (int i = 0; i < records.Count; i++)
        {
            if (i == start) continue;
            try
            {
                merged = BcrdtUpdateOperations.Merge(merged, records[i].Update);
                merging.Add(records[i]);
            }
            catch (BcrdtFormatException)
            {
                // Left out.
            }
        }

        merging.Sort((a, b) => a.Id.CompareTo(b.Id));
        return (merging, merged);
    }

    private static bool IsDecodable(byte[] update)
    {
        try
        {
            // Reads the blocks and the delete set: what a document would decode.
            BcrdtUpdateOperations.DecodeDeleteSet(update);
            return true;
        }
        catch (BcrdtFormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Runs one write as a transaction of the store's own, and commits it.
    /// </summary>
    /// <remarks>
    /// BlazeDb lets one transaction be open at a time and has every write join it. Writing while the application
    /// had one open - across an <c>await</c>, say - would put the store's record into it, and an application
    /// rollback would then take back an update the store had reported stored. Opening a transaction of its own
    /// refuses that case instead, as a transient failure: the persistence layer keeps the update and retries it.
    /// </remarks>
    /// <returns>The exception an application <c>Changed</c> handler threw after the commit, if one did.</returns>
    private static BlazeDbChangeHandlerException? Commit(BlazeDbDatabase database, Action write)
    {
        BlazeDbTransaction transaction;
        try
        {
            transaction = database.BeginTransaction();
        }
        catch (InvalidOperationException ex) when (ex is not ObjectDisposedException)
        {
            throw new IOException(
                "The application has a BlazeDb transaction open on the database the documents are kept in, so the store " +
                "cannot write without joining it. The write is retried once the transaction ends.", ex);
        }

        using (transaction)
        {
            write();

            try
            {
                transaction.Commit();
            }
            catch (BlazeDbChangeHandlerException ex)
            {
                // Committed: the exception is the application's handler, which ran afterwards.
                return ex;
            }
        }

        return null;
    }

    private async ValueTask FinishAsync(
        BlazeDbDatabase database,
        DatabaseState state,
        BlazeDbChangeHandlerException? handlerFailure,
        CancellationToken cancellationToken)
    {
        if (handlerFailure is not null) ChangeHandlerFailed?.Invoke(handlerFailure);
        if (_options.WaitForDurability) await FlushAsync(database, state, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Waits for the database to put what it has committed into its storage.</summary>
    /// <remarks>
    /// After a write, outside the lock: a flush serializes with writes inside BlazeDb, and reads nothing of the
    /// store's. A failure leaves the commit in the database's memory, where BlazeDb's own flusher keeps retrying it; it is
    /// reported as transient, so the caller retries too - and that retry flushes before committing anything new.
    /// </remarks>
    private static async ValueTask FlushAsync(BlazeDbDatabase database, DatabaseState state, CancellationToken cancellationToken)
    {
        state.Unflushed = true;
        try
        {
            await database.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not ObjectDisposedException)
        {
            throw new IOException(
                "The change was committed to the database but could not be flushed to its storage. BlazeDb keeps it in " +
                "memory and keeps retrying the flush; database.LastBackgroundError reports the standing failure.", ex);
        }
        state.Unflushed = false;
    }

    private static void RequireWritable(BlazeDbDatabase database)
    {
        if (database.IsReadOnly)
        {
            throw new NotSupportedException(
                "The database is a read-only BlazeDb replica: documents load from it, but their changes can only be stored by " +
                "the tab holding the writer lock.");
        }
    }

    private static void Validate(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        try
        {
            StrictUtf8.GetByteCount(documentId);
        }
        catch (EncoderFallbackException ex)
        {
            throw new ArgumentException(
                "The document identifier is not well-formed Unicode, so it could not be stored and read back as the same string.",
                nameof(documentId), ex);
        }
    }

    /// <summary>What every store on one database shares.</summary>
    private sealed class DatabaseState
    {
        private readonly object _sync = new();
        private long _lastId;
        private bool _counted;
        private volatile bool _unflushed;

        /// <summary>The lock the stores on this database are serialized with, unless given one.</summary>
        public SemaphoreSlim Gate { get; } = new(1, 1);

        /// <summary>Whether a commit was made whose flush has not been seen to succeed.</summary>
        public bool Unflushed
        {
            get => _unflushed;
            set => _unflushed = value;
        }

        /// <summary>The next unused record number, counted from the table the first time it is asked for.</summary>
        public long NextId(BlazeDbTable<long, BcrdtBlazeDbUpdateRecord> table)
        {
            lock (_sync)
            {
                if (!_counted)
                {
                    _lastId = table.Scan().Select(record => record.Id).DefaultIfEmpty(0).Max();
                    _counted = true;
                }
                return ++_lastId;
            }
        }

        /// <summary>Counts the table afresh on the next <see cref="NextId"/>.</summary>
        public void Reset()
        {
            lock (_sync) _counted = false;
        }
    }
}
