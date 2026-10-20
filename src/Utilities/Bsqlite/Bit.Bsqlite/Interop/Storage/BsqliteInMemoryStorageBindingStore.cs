namespace Bit.Bsqlite.Interop;

/// <summary>
/// A binding store that remembers nothing beyond the current session.
/// </summary>
/// <remarks>
/// The real store has to outlive the page - the whole point is to know where a database went before
/// this session started - so this is for tests and for configurations where every backend is itself
/// volatile. Production registers a store backed by durable browser storage.
/// </remarks>
public sealed class BsqliteInMemoryStorageBindingStore : IBsqliteStorageBindingStore
{
    private readonly Dictionary<string, string> _bindings = new(StringComparer.Ordinal);

    public ValueTask<string?> GetProviderNameAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        return ValueTask.FromResult(
            _bindings.TryGetValue(databaseName, out var providerName) ? providerName : null);
    }

    public ValueTask SetProviderNameAsync(
        string databaseName,
        string providerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        _bindings[databaseName] = providerName;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        _bindings.Remove(databaseName);
        return ValueTask.CompletedTask;
    }
}
