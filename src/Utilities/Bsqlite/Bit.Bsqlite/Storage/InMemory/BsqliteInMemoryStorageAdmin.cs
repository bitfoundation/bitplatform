using System.Collections.Concurrent;
using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Storage.InMemory;

/// <summary>
/// Tracks the in-memory backend's databases and their file images.
/// </summary>
/// <remarks>
/// Export and import are real against this store, but the store is not the engine's. The backend
/// registers no VFS, so a database opened through a connection lives in SQLite's built-in memory
/// VFS inside the worker and never appears here. See
/// <see cref="BsqliteInMemoryStorageProvider"/>.
/// </remarks>
internal sealed class BsqliteInMemoryStorageAdmin : IBsqliteStorageAdmin
{
    private readonly ConcurrentDictionary<string, byte[]> _databases = new(StringComparer.Ordinal);

    public ValueTask<bool> ExistsAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        return ValueTask.FromResult(_databases.ContainsKey(databaseName));
    }

    public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyList<string>>([.. _databases.Keys.Order(StringComparer.Ordinal)]);

    public ValueTask DeleteAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        _databases.TryRemove(databaseName, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask<byte[]> ExportAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        if (!_databases.TryGetValue(databaseName, out var image))
        {
            throw new FileNotFoundException(
                $"In-memory storage holds no database named '{databaseName}'.",
                databaseName);
        }

        // Copied so a caller cannot mutate the stored image through the array it was handed.
        return ValueTask.FromResult(image.ToArray());
    }

    public ValueTask ImportAsync(
        string databaseName,
        ReadOnlyMemory<byte> contents,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        _databases[databaseName] = contents.ToArray();
        return ValueTask.CompletedTask;
    }
}
