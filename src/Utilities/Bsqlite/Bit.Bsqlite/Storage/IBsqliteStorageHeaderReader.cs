namespace Bit.Bsqlite;

/// <summary>
/// An admin that can read a database's 100-byte SQLite header without reading the rest of it.
/// </summary>
/// <remarks>
/// What a migration checks first against <see cref="IBsqliteStorageImportCheck"/>: a database the target refuses
/// is then refused before the whole of it is exported - an export that, on the automatic path, would otherwise
/// be repeated on every open.
/// </remarks>
internal interface IBsqliteStorageHeaderReader
{
    /// <summary>
    /// The first bytes of <paramref name="databaseName"/>, up to the 100 of the SQLite header; null when it
    /// does not exist.
    /// </summary>
    ValueTask<byte[]?> ReadHeaderAsync(string databaseName, CancellationToken cancellationToken = default);
}
