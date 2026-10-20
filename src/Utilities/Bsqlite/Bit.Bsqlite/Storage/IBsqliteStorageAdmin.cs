namespace Bit.Bsqlite.Storage;

/// <summary>
/// Operations on databases as whole artifacts, outside any SQL connection: what exists, deleting,
/// and moving bytes in and out.
/// </summary>
/// <remarks>
/// Separated from the provider itself because these are the operations cross-provider migration and
/// the diagnostics surface are built from, and because a backend can implement them without the
/// engine running.
/// </remarks>
public interface IBsqliteStorageAdmin
{
    /// <summary>Whether <paramref name="databaseName"/> exists in this backend.</summary>
    ValueTask<bool> ExistsAsync(string databaseName, CancellationToken cancellationToken = default);

    /// <summary>Names of every database this backend holds. Used by diagnostics and migration.</summary>
    ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes <paramref name="databaseName"/>, succeeding quietly when it is already absent.
    /// </summary>
    ValueTask DeleteAsync(string databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the database out as a SQLite file image, for backup or for the copy step of a
    /// cross-provider migration.
    /// </summary>
    ValueTask<byte[]> ExportAsync(string databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a SQLite file image in, replacing any existing database of that name.
    /// </summary>
    /// <remarks>
    /// Callers are expected to verify the result - the migration protocol runs
    /// <c>PRAGMA integrity_check</c> against the copy before it trusts it.
    /// </remarks>
    ValueTask ImportAsync(
        string databaseName,
        ReadOnlyMemory<byte> contents,
        CancellationToken cancellationToken = default);
}
