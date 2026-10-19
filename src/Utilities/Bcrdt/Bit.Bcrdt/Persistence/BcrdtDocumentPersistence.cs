using System.Threading.Channels;

namespace Bit.Bcrdt;

/// <summary>Settings for binding a document to a store.</summary>
public sealed class BcrdtDocumentPersistenceOptions
{
    /// <summary>How many records may accumulate before the log is compacted.</summary>
    public int CompactionThreshold { get; init; } = 500;

    /// <summary>
    /// A tag identifying writes this component made, so they are not written straight back out.
    /// </summary>
    public static object LoadOrigin { get; } = new BcrdtDocumentPersistenceOrigin();

    /// <summary>Whether updates that arrived from peers are persisted too.</summary>
    /// <remarks>
    /// Normally yes: a client that persists only its own edits cannot show the document offline.
    /// Turn it off only on a server that already has the authoritative copy elsewhere.
    /// </remarks>
    public bool PersistRemoteUpdates { get; init; } = true;

    /// <summary>How long to wait before retrying a failed write. Doubles on each consecutive failure.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>The longest wait between retries of a failed write.</summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>The marker type used to tag updates a persistence layer applied.</summary>
public sealed class BcrdtDocumentPersistenceOrigin
{
    /// <inheritdoc/>
    public override string ToString() => "persistence";
}

/// <summary>
/// Keeps a document and a store in sync, in the background.
/// </summary>
/// <remarks>
/// <para>
/// Attaching does two things: it replays whatever the store holds into the document, and it starts
/// writing every subsequent change back. Because updates merge, replaying is a single
/// <c>ApplyUpdate</c> rather than a replay loop, and it converges with whatever the document already
/// contained rather than overwriting it - which is what makes "open offline, then reconnect" work.
/// </para>
/// <para>
/// Writes are queued and coalesced: whatever accumulates while a write is in progress becomes one
/// record, so a burst of keystrokes against a slow store costs one record rather than one each, and
/// the UI thread never waits on storage.
/// </para>
/// <para>
/// A write that fails transiently is kept and retried, with backoff, together with whatever arrives
/// meanwhile. It is not skipped: the document's updates form a causal chain per client, so a single
/// missing record would leave every later change by the same client waiting forever for it when the
/// log is loaded. Only an update the store refuses outright - which no retry could change - is
/// dropped, and that is reported through <see cref="WriteFailed"/> and <see cref="FlushAsync"/>.
/// </para>
/// </remarks>
public sealed class BcrdtDocumentPersistence : IAsyncDisposable
{
    private readonly BcrdtDocument _doc;
    private readonly IBcrdtDocumentStore _store;
    private readonly string _documentId;
    private readonly BcrdtDocumentPersistenceOptions _options;
    private readonly Channel<byte[]> _queue;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _writerLoop;
    private readonly object _idleLock = new();

    // The most a coalesced record may grow to: far below any store's record limit, so a batch that
    // waits through a failure never becomes a record the store will refuse.
    private const int MaxCoalescedBytes = 4 * 1024 * 1024;

    private TaskCompletionSource _idle = CreateCompletedSource();
    private Exception? _unstored;
    private int _queued;
    private int _recordCount;
    private bool _disposed;

    private BcrdtDocumentPersistence(
        BcrdtDocument doc,
        IBcrdtDocumentStore store,
        string documentId,
        BcrdtDocumentPersistenceOptions options)
    {
        _doc = doc;
        _store = store;
        _documentId = documentId;
        _options = options;
        _queue = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });
        _writerLoop = Task.Run(WriteLoopAsync);
    }

    /// <summary>Loads a document from a store and keeps it written back from then on.</summary>
    public static async ValueTask<BcrdtDocumentPersistence> AttachAsync(
        BcrdtDocument doc,
        IBcrdtDocumentStore store,
        string? documentId = null,
        BcrdtDocumentPersistenceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(store);

        var persistence = new BcrdtDocumentPersistence(
            doc, store, documentId ?? doc.Id, options ?? new BcrdtDocumentPersistenceOptions());

        try
        {
            BcrdtStoredDocument stored = await store.LoadAsync(persistence._documentId, cancellationToken).ConfigureAwait(false);
            if (stored.Update is not null)
            {
                doc.ApplyUpdate(stored.Update, BcrdtDocumentPersistenceOptions.LoadOrigin);
                persistence._recordCount = stored.RecordCount;
            }

            // Anything the document already held before loading has not been written yet. Nothing
            // is written when the document holds nothing the store lacks, so opening a document
            // does not add a record to its log every time.
            byte[]? unpersisted = Unpersisted(doc, stored.Update);
            if (unpersisted is not null) persistence.Enqueue(unpersisted);

            doc.Updated += persistence.OnDocumentUpdated;
            return persistence;
        }
        catch
        {
            await persistence.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>How many records the store currently holds for this document.</summary>
    public int RecordCount => Volatile.Read(ref _recordCount);

    /// <summary>Raised when a write to the store fails.</summary>
    /// <remarks>
    /// Storage failures are reported rather than thrown, because they happen on a background loop
    /// and must not take down an editing session. A transient failure is retried until it succeeds,
    /// so it reports a delay in durability, not a loss - unless the binding is disposed before the
    /// store recovers. A permanent one (the store refused the record as invalid) means that update
    /// could not be stored. Raised on a background thread.
    /// </remarks>
    public event Action<Exception>? WriteFailed;

    /// <summary>Waits until every queued change has reached the store.</summary>
    /// <remarks>
    /// While the store is failing transiently this keeps waiting, because the changes are still being
    /// retried. The returned task faults if a change the store refused outright could not be stored,
    /// or if the binding is disposed with changes that never reached the store.
    /// </remarks>
    public Task FlushAsync()
    {
        lock (_idleLock)
        {
            return _idle.Task;
        }
    }

    private static byte[]? Unpersisted(BcrdtDocument doc, byte[]? stored)
    {
        if (stored is null)
        {
            byte[] everything = doc.EncodeStateAsUpdate();
            return BcrdtUpdateOperations.IsEmpty(everything) ? null : everything;
        }

        BcrdtStateVector storedVector = BcrdtUpdateOperations.DecodeStateVector(stored);
        bool newContent = !storedVector.IsSupersetOf(doc.GetStateVector());
        bool newDeletions = !Covers(BcrdtUpdateOperations.DecodeDeleteSet(stored), doc.GetDeleteSet());
        if (!newContent && !newDeletions) return null;

        byte[] delta = doc.EncodeStateAsUpdate(storedVector);
        return BcrdtUpdateOperations.IsEmpty(delta) ? null : delta;
    }

    /// <summary>Whether every deletion in <paramref name="subset"/> is also in <paramref name="superset"/>.</summary>
    private static bool Covers(BcrdtDeleteSet superset, BcrdtDeleteSet subset)
    {
        superset.SortAndMerge();
        BcrdtDeleteSet candidate = subset.Clone();
        candidate.SortAndMerge();

        foreach ((ulong client, List<BcrdtDeleteRange> ranges) in candidate.Clients)
        {
            if (!superset.Clients.TryGetValue(client, out List<BcrdtDeleteRange>? covering)) return false;

            int index = 0;
            foreach (BcrdtDeleteRange range in ranges)
            {
                while (index < covering.Count && covering[index].End <= range.Clock) index++;
                if (index >= covering.Count || covering[index].Clock > range.Clock || covering[index].End < range.End)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void OnDocumentUpdated(BcrdtUpdateEventArgs args)
    {
        if (ReferenceEquals(args.Origin, BcrdtDocumentPersistenceOptions.LoadOrigin)) return;
        if (!_options.PersistRemoteUpdates && !args.Transaction.Local) return;
        Enqueue(args.Update);
    }

    private void Enqueue(byte[] update)
    {
        lock (_idleLock)
        {
            if (_queued++ == 0)
            {
                _idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _unstored = null;
            }
        }

        if (!_queue.Writer.TryWrite(update))
        {
            // Shut down already; nothing will write it.
            Complete(1, new IOException($"Persistence for '{_documentId}' has been shut down."));
        }
    }

    /// <summary>
    /// Whether a failed write can never succeed, however often it is retried.
    /// </summary>
    /// <remarks>
    /// A store refuses a record it considers invalid - one over its size limit, say - with an argument
    /// or not-supported error, and an update that cannot be decoded will never merge. Anything else
    /// - an I/O error, a timeout, a cancellation the store raised itself - may clear, and is retried.
    /// </remarks>
    private static bool IsPermanent(Exception exception) =>
        exception is ArgumentException or NotSupportedException or BcrdtFormatException or InvalidDataException;

    private async Task WriteLoopAsync()
    {
        CancellationToken token = _shutdown.Token;

        // Updates taken from the queue but not yet stored, oldest first. They are kept as they arrived
        // rather than as one merged record, so a record that keeps failing does not grow with every
        // update that queues behind it.
        List<byte[]> pending = [];

        // After a coalesced record was refused, how many leading updates to write one at a time, so the
        // one the store cannot accept is found and dropped instead of taking the rest with it.
        int separately = 0;
        TimeSpan delay = _options.RetryDelay;

        try
        {
            while (true)
            {
                if (pending.Count == 0 && !await _queue.Reader.WaitToReadAsync(token).ConfigureAwait(false)) break;

                while (_queue.Reader.TryRead(out byte[]? update)) pending.Add(update);
                if (pending.Count == 0) continue;

                // Coalescing here is what keeps a burst of keystrokes to one record - within a size
                // limit well below any store's record limit.
                int take = 1;
                if (separately == 0)
                {
                    long size = pending[0].Length;
                    while (take < pending.Count && size + pending[take].Length <= MaxCoalescedBytes)
                    {
                        size += pending[take].Length;
                        take++;
                    }
                }

                try
                {
                    byte[] record = take == 1
                        ? pending[0]
                        : BcrdtUpdateOperations.Merge([.. pending.Take(take).Select(p => (ReadOnlyMemory<byte>)p)]);

                    await _store.AppendUpdateAsync(_documentId, record, token).ConfigureAwait(false);
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    ReportFailure(ex);

                    if (IsPermanent(ex))
                    {
                        if (take > 1)
                        {
                            separately = take;
                            continue;
                        }

                        // This update will never be accepted. Retrying it would only wedge everything
                        // queued behind it, so it is dropped - reported above, and surfaced through
                        // FlushAsync - and the rest carry on.
                        pending.RemoveAt(0);
                        if (separately > 0) separately--;
                        Complete(1, ex);
                        continue;
                    }

                    // Includes cancellations raised by the store itself, such as an HTTP timeout: only
                    // our own shutdown ends the loop. Keep the data and try again.
                    await Task.Delay(delay, token).ConfigureAwait(false);
                    delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, _options.MaxRetryDelay.Ticks));
                    continue;
                }

                delay = _options.RetryDelay;
                pending.RemoveRange(0, take);
                separately = Math.Max(0, separately - take);
                Interlocked.Increment(ref _recordCount);
                Complete(take, failure: null);

                if (Volatile.Read(ref _recordCount) >= _options.CompactionThreshold)
                {
                    try
                    {
                        await _store.CompactAsync(_documentId, token).ConfigureAwait(false);
                        Volatile.Write(ref _recordCount, 1);
                    }
                    catch (Exception ex) when (!token.IsCancellationRequested)
                    {
                        // Compaction is an optimization; the log is intact. Try again after the next write.
                        ReportFailure(ex);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private void ReportFailure(Exception exception)
    {
        try
        {
            WriteFailed?.Invoke(exception);
        }
        catch (Exception)
        {
            // A subscriber that throws must not stop the writer; the failure is still retried.
        }
    }

    private void Complete(int count, Exception? failure)
    {
        lock (_idleLock)
        {
            if (failure is not null) _unstored ??= failure;

            _queued -= count;
            if (_queued > 0) return;
            _queued = 0;

            // Every queued change has been dealt with. The flush succeeds if all of them were stored,
            // and faults - rather than hanging - if any could not be.
            if (_unstored is null) _idle.TrySetResult();
            else _idle.TrySetException(new IOException($"Changes to '{_documentId}' could not be stored.", _unstored));
            _unstored = null;
        }
    }

    private static TaskCompletionSource CreateCompletedSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _doc.Updated -= OnDocumentUpdated;
        _queue.Writer.TryComplete();

        // Give queued writes a bounded chance to land before tearing the loop down; durability is
        // worth a moment at shutdown, but a wedged store must not hang the application.
        try
        {
            await _writerLoop.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            await _shutdown.CancelAsync().ConfigureAwait(false);
            try
            {
                await _writerLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Already shutting down.
            }
        }

        // Anyone still waiting for a flush must not wait forever for writes that will never happen.
        lock (_idleLock)
        {
            if (_queued > 0)
            {
                _queued = 0;
                _idle.TrySetException(new IOException(
                    $"Changes to '{_documentId}' could not be stored before persistence was shut down."));
            }
        }

        _shutdown.Dispose();
    }
}
