namespace Bit.BlazeDb.Storage;

/// <summary>
/// Minimal storage abstraction the engine persists through. Implementations: OPFS (browser),
/// file system (desktop/tests), in-memory (tests). All operations are asynchronous because the
/// browser backend is; the engine never blocks a caller on storage.
/// </summary>
public interface IBlazeDbStorage
{
    /// <summary>Reads an entire file, or null if it does not exist.</summary>
    ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a file's contents atomically: after a crash the file contains either the old
    /// or the new content, never a mix.
    /// </summary>
    ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>Appends bytes to the end of a file, creating it if missing.</summary>
    ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>Deletes a file if it exists.</summary>
    ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default);
}
