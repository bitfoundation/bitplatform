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
public sealed class BcrdtInMemoryDocumentStore : IBcrdtDocumentStore
{
    private readonly ConcurrentDictionary<string, Log> _documents = new(StringComparer.Ordinal);
    private readonly int _maxDocuments;
    private long _clock;

    /// <summary>How recently a document must have been used to be safe from eviction.</summary>
    private const long EvictionGraceMilliseconds = 60_000;

    /// <summary>Creates a store that keeps every document it is given.</summary>
    public BcrdtInMemoryDocumentStore() : this(0) { }

    /// <summary>Creates a store that keeps at most <paramref name="maxDocuments"/> documents.</summary>
    /// <param name="maxDocuments">
    /// How many documents to keep; the one used least recently is dropped to make room for a new one.
    /// Zero keeps every document. A bound is what makes the store safe behind something anyone can create
    /// documents through - a public demo, a room per visitor - where an unbounded one would grow with every
    /// identifier ever used until the process ran out of memory.
    /// </param>
    public BcrdtInMemoryDocumentStore(int maxDocuments)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxDocuments);
        _maxDocuments = maxDocuments;
    }

    /// <summary>How many documents the store holds.</summary>
    public int Count => _documents.Count;

    /// <inheritdoc/>
    public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (!_documents.TryGetValue(documentId, out Log? log)) return new ValueTask<BcrdtStoredDocument>(default(BcrdtStoredDocument));

        byte[][] snapshot;
        lock (log)
        {
            if (log.Records.Count == 0) return new ValueTask<BcrdtStoredDocument>(default(BcrdtStoredDocument));
            log.LastUsed = Interlocked.Increment(ref _clock);
            log.LastUsedAt = Environment.TickCount64;
            snapshot = [.. log.Records];
        }

        // A copy, so a caller that modifies the buffer cannot corrupt what the store holds.
        byte[] merged = snapshot.Length == 1
            ? (byte[])snapshot[0].Clone()
            : BcrdtUpdateOperations.Merge([.. snapshot.Select(r => (ReadOnlyMemory<byte>)r)]);

        return new ValueTask<BcrdtStoredDocument>(new BcrdtStoredDocument(merged, snapshot.Length));
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
                log.LastUsed = Interlocked.Increment(ref _clock);
                log.LastUsedAt = Environment.TickCount64;
            }

            if (_maxDocuments > 0 && _documents.Count > _maxDocuments) EvictLeastRecentlyUsed(log);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Drops the documents used longest ago until the store is back within its bound - but never one used
    /// in the last minute.
    /// </summary>
    /// <remarks>
    /// A document still being written to - open somewhere, with its writer appending - must not lose its
    /// log: the next record would start a new one holding changes without what they build on, and the
    /// document would load empty. While more documents than the bound are that busy, the store holds more
    /// than the bound.
    /// </remarks>
    private void EvictLeastRecentlyUsed(Log keep)
    {
        long now = Environment.TickCount64;
        while (_documents.Count > _maxDocuments)
        {
            KeyValuePair<string, Log>? oldest = null;
            foreach (KeyValuePair<string, Log> entry in _documents)
            {
                if (ReferenceEquals(entry.Value, keep) || now - entry.Value.LastUsedAt < EvictionGraceMilliseconds) continue;
                if (oldest is null || entry.Value.LastUsed < oldest.Value.Value.LastUsed) oldest = entry;
            }

            if (oldest is not { } victim) return;
            if (_documents.TryRemove(victim))
            {
                lock (victim.Value) victim.Value.Deleted = true;
            }
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

        /// <summary>When the document was last read or written, by <see cref="Environment.TickCount64"/>.</summary>
        public long LastUsedAt { get; set; }
    }
}
