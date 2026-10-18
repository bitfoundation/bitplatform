namespace Bit.Bsqlite.Data;

/// <summary>
/// A transport that can hand out a consistent snapshot of the open database as a SQLite file image
/// without closing it.
/// </summary>
/// <remarks>
/// Optional because not every transport can: the worker takes the snapshot through
/// <c>sqlite3_serialize</c>, which copies every committed page under the engine's own lock, so it
/// is safe while the database is in use.
/// </remarks>
public interface IBsqliteSnapshotSource
{
    /// <summary>Reads the whole database out as a SQLite file image.</summary>
    Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);
}
