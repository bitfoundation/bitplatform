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
                return ValueTask.CompletedTask;
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
    }
}
