using System.Collections.Concurrent;

namespace Bit.Bink;

/// <summary>
/// A store in memory: for tests, for a server that keeps recent drawings per user, and as the simplest example of the
/// contract. Thread-safe. Nothing survives the process.
/// </summary>
public sealed class BinkMemoryDocumentStore : IBinkDocumentStore
{
    private readonly ConcurrentDictionary<string, byte[]> _documents = new(StringComparer.Ordinal);

    /// <summary>The keys currently stored.</summary>
    public IReadOnlyCollection<string> Keys => _documents.Keys.ToArray();

    /// <inheritdoc />
    public Task SaveAsync(string key, ReadOnlyMemory<byte> document, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();
        _documents[key] = document.ToArray();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<byte[]?> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_documents.TryGetValue(key, out byte[]? bytes) ? bytes.ToArray() : null);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_documents.TryRemove(key, out _));
    }
}
