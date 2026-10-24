using System.Collections.Concurrent;

namespace Bit.Bcrdt;

/// <summary>
/// A store that keeps everything in process memory.
/// </summary>
/// <remarks>
/// Useful in three places: tests, where it makes persistence behaviour deterministic and fast; a
/// server that holds hot documents in memory and flushes elsewhere; and as the reference
/// implementation to read when writing a real backend, since it is short enough to take in at once.
/// Each document's records are guarded by their own lock, and compaction merges and swaps them in one
/// step, so concurrent appends and compactions - two sessions persisting the same document - never
/// lose a record.
/// </remarks>
public sealed class BcrdtInMemoryDocumentStore : IBcrdtDocumentStore, IBcrdtAttachedDocumentTracker
{
    /// <summary>How recently a document must have been used to be safe from eviction.</summary>
    private static readonly TimeSpan EvictionGrace = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, Log> _documents = new(StringComparer.Ordinal);
    private readonly int _maxDocuments;
    private readonly TimeProvider _time;
    private long _clock;

    // How many persistence bindings each document is attached to. Guarded by itself; eviction reads it
    // while holding the candidate's log, so a document attached concurrently is either seen as attached
    // or finds its log already gone when it loads.
    private readonly Dictionary<string, int> _attached = new(StringComparer.Ordinal);

    /// <summary>Creates a store that keeps every document it is given.</summary>
    public BcrdtInMemoryDocumentStore() : this(0) { }

    /// <summary>Creates a store that keeps at most <paramref name="maxDocuments"/> documents.</summary>
    /// <param name="maxDocuments">
    /// How many documents to keep; the one used least recently is dropped to make room for a new one.
    /// Zero keeps every document. A bound is what makes the store safe behind something anyone can create
    /// documents through - a public demo, a room per visitor - where an unbounded one would grow with every
    /// identifier ever used until the process ran out of memory. A document still attached to a
    /// <see cref="BcrdtDocumentPersistence"/> - open in a document host or a collaboration service - is
    /// never dropped, so while more documents than the bound are open the store holds more than the bound.
    /// </param>
    public BcrdtInMemoryDocumentStore(int maxDocuments) : this(maxDocuments, TimeProvider.System) { }

    internal BcrdtInMemoryDocumentStore(int maxDocuments, TimeProvider timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxDocuments);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _maxDocuments = maxDocuments;
        _time = timeProvider;
    }

    /// <summary>How many documents the store holds.</summary>
    public int Count => _documents.Count;

    /// <inheritdoc/>
    public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (!_documents.TryGetValue(documentId, out Log? log)) return new ValueTask<BcrdtStoredDocument>(default(BcrdtStoredDocument));

            byte[][] snapshot;
            lock (log)
            {
                // Evicted or deleted between the lookup and the lock. Handing out what it held would let
                // the caller append to a fresh log that lacks it; look again instead.
                if (log.Deleted) continue;
                if (log.Records.Count == 0) return new ValueTask<BcrdtStoredDocument>(default(BcrdtStoredDocument));
                Touch(log);
                snapshot = [.. log.Records];
            }

            // A copy, so a caller that modifies the buffer cannot corrupt what the store holds.
            byte[] merged = snapshot.Length == 1
                ? (byte[])snapshot[0].Clone()
                : BcrdtUpdateOperations.Merge([.. snapshot.Select(r => (ReadOnlyMemory<byte>)r)]);

            return new ValueTask<BcrdtStoredDocument>(new BcrdtStoredDocument(merged, snapshot.Length));
        }
    }

    /// <inheritdoc/>
    public ValueTask AppendUpdateAsync(
        string documentId,
        ReadOnlyMemory<byte> update,
        CancellationToken cancellationToken = default)
    {
        byte[] record = update.ToArray();
        while (true)
        {
            Log log = _documents.GetOrAdd(documentId, static _ => new Log());
            lock (log)
            {
                // Deleted between the lookup and the lock: append to the replacement instead, or the
                // record would vanish with the discarded log.
                if (log.Deleted) continue;
                log.Records.Add(record);
                Touch(log);
            }

            if (_maxDocuments > 0 && _documents.Count > _maxDocuments) EvictLeastRecentlyUsed(log);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Drops the documents used longest ago until the store is back within its bound - but never one
    /// that is attached, and never one used in the last minute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A document still in use must not lose its log: the next record would start a new one holding
    /// changes without what they build on, and the document would load empty or partial. A document
    /// attached to a <see cref="BcrdtDocumentPersistence"/> is in use for as long as it stays attached,
    /// however long it sits idle; the minute's grace covers callers that append without one.
    /// </para>
    /// <para>
    /// The victim is removed under its own lock, and only if nothing used it since it was chosen, so an
    /// append that races the eviction either lands first and saves the document, or finds the log gone
    /// and starts the replacement - it is never reported stored and then dropped with the log.
    /// </para>
    /// </remarks>
    private void EvictLeastRecentlyUsed(Log keep)
    {
        while (_documents.Count > _maxDocuments)
        {
            KeyValuePair<string, Log>? oldest = null;
            long oldestUse = 0;
            foreach (KeyValuePair<string, Log> entry in _documents)
            {
                if (ReferenceEquals(entry.Value, keep) || IsAttached(entry.Key)) continue;

                long lastUsed;
                long lastUsedAt;
                lock (entry.Value)
                {
                    lastUsed = entry.Value.LastUsed;
                    lastUsedAt = entry.Value.LastUsedAt;
                }

                if (_time.GetElapsedTime(lastUsedAt) < EvictionGrace) continue;
                if (oldest is null || lastUsed < oldestUse)
                {
                    oldest = entry;
                    oldestUse = lastUsed;
                }
            }

            if (oldest is not { } victim) return;

            lock (victim.Value)
            {
                // Used or attached since it was chosen: choose again.
                if (victim.Value.Deleted || victim.Value.LastUsed != oldestUse || IsAttached(victim.Key)) continue;
                if (_documents.TryRemove(victim)) victim.Value.Deleted = true;
            }
        }
    }

    private void Touch(Log log)
    {
        log.LastUsed = Interlocked.Increment(ref _clock);
        log.LastUsedAt = _time.GetTimestamp();
    }

    private bool IsAttached(string documentId)
    {
        lock (_attached) return _attached.ContainsKey(documentId);
    }

    void IBcrdtAttachedDocumentTracker.OnAttached(string documentId)
    {
        lock (_attached)
        {
            _attached[documentId] = _attached.TryGetValue(documentId, out int count) ? count + 1 : 1;
        }
    }

    void IBcrdtAttachedDocumentTracker.OnDetached(string documentId)
    {
        lock (_attached)
        {
            if (!_attached.TryGetValue(documentId, out int count)) return;
            if (count > 1) _attached[documentId] = count - 1;
            else _attached.Remove(documentId);
        }
    }

    /// <inheritdoc/>
    public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (!_documents.TryGetValue(documentId, out Log? log)) return ValueTask.CompletedTask;

        lock (log)
        {
            if (log.Deleted || log.Records.Count <= 1) return ValueTask.CompletedTask;

            byte[] merged = BcrdtUpdateOperations.Merge([.. log.Records.Select(r => (ReadOnlyMemory<byte>)r)]);
            log.Records.Clear();
            log.Records.Add(merged);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (_documents.TryRemove(documentId, out Log? log))
        {
            lock (log) log.Deleted = true;
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
        new((IReadOnlyList<string>)_documents.Keys.ToArray());

    private sealed class Log
    {
        public List<byte[]> Records { get; } = [];

        public bool Deleted { get; set; }

        /// <summary>When the document was last read or written, by the store's own counter.</summary>
        public long LastUsed { get; set; }

        /// <summary>When the document was last read or written, as a <see cref="TimeProvider"/> timestamp.</summary>
        public long LastUsedAt { get; set; }
    }
}
