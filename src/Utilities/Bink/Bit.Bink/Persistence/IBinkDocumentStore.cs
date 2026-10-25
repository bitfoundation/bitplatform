using System.Collections.Concurrent;

namespace Bit.Bink;

/// <summary>
/// Somewhere to keep serialized documents by key: the browser's own storage, a database, a file system, an
/// application's API. <see cref="BinkAutosave"/> writes to one; implement it to put autosaved drawings wherever the
/// application keeps its data.
/// </summary>
/// <remarks>
/// A store holds bytes, never documents, so it needs no knowledge of the format and every implementation behaves the
/// same. Implementations must accept being called again before a previous call finished, for different keys at least.
/// </remarks>
public interface IBinkDocumentStore
{
    /// <summary>Stores a serialized document under a key, replacing what was there.</summary>
    Task SaveAsync(string key, ReadOnlyMemory<byte> document, CancellationToken cancellationToken = default);

    /// <summary>Returns the serialized document stored under a key, or null when there is none.</summary>
    Task<byte[]?> LoadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes what is stored under a key. Returns false when there was nothing.</summary>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
}

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
