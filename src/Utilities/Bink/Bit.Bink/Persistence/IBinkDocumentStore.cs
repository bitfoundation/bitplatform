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
