namespace Bit.Bsqlite;

/// <summary>
/// Remembers which storage backend holds each database, so that selection can put existing data
/// ahead of configured preference.
/// </summary>
/// <remarks>
/// This is the data-loss guard. Without it, a database created on IndexedDB would be shadowed by a
/// fresh, empty OPFS database the moment a browser update made OPFS available - the application would
/// see an empty database and no error. The store therefore has to live somewhere no single backend
/// owns, and it holds names only, never data.
/// </remarks>
public interface IBsqliteStorageBindingStore
{
    /// <summary>
    /// The provider that holds <paramref name="databaseName"/>, or <see langword="null"/> if this
    /// database has not been created yet.
    /// </summary>
    ValueTask<string?> GetProviderNameAsync(
        string databaseName,
        CancellationToken cancellationToken = default);

    /// <summary>Records that <paramref name="databaseName"/> lives on <paramref name="providerName"/>.</summary>
    ValueTask SetProviderNameAsync(
        string databaseName,
        string providerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forgets <paramref name="databaseName"/>, which is what deleting a database must do so a later
    /// create is free to pick the best backend again.
    /// </summary>
    ValueTask RemoveAsync(string databaseName, CancellationToken cancellationToken = default);
}
