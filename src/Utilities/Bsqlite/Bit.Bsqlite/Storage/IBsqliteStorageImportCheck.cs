namespace Bit.Bsqlite;

/// <summary>An admin that keeps only some databases, and can tell which from their header.</summary>
internal interface IBsqliteStorageImportCheck
{
    /// <summary>
    /// Throws <see cref="NotSupportedException"/> when a database whose image starts with
    /// <paramref name="header"/> cannot be imported here.
    /// </summary>
    void ThrowIfCannotImport(ReadOnlySpan<byte> header);
}
