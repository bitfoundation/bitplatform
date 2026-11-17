using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Bit.Bsqlite;

/// <summary>
/// Database-level operations against the IndexedDB the VFS uses, for diagnostics and migration.
/// </summary>
/// <remarks>
/// Talks to <c>bit-bsqlite-idb-admin.js</c>. Related files (<c>-journal</c>, <c>-wal</c>) are not databases
/// and are stripped on list, delete, and import.
/// </remarks>
internal sealed class BsqliteIndexedDbStorageAdmin(IJSRuntime? js) : IBsqliteStorageAdmin, IBsqliteStorageHeaderReader, IBsqliteStorageImportCheck
{
    public async ValueTask<bool> ExistsAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        return await InvokeAsync<bool>("exists", cancellationToken, databaseName).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var names = await InvokeAsync<string[]>("list", cancellationToken).ConfigureAwait(false);
        return names;
    }

    public async ValueTask DeleteAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        await InvokeAsync("deleteDatabase", cancellationToken, databaseName).ConfigureAwait(false);
    }

    public async ValueTask<byte[]> ExportAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        var bytes = await InvokeAsync<byte[]>("exportDatabase", cancellationToken, databaseName)
            .ConfigureAwait(false);
        return bytes;
    }

    public async ValueTask<byte[]?> ReadHeaderAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        return await InvokeAsync<byte[]?>("readHeader", cancellationToken, databaseName).ConfigureAwait(false);
    }

    public async ValueTask ImportAsync(
        string databaseName,
        ReadOnlyMemory<byte> contents,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ThrowIfPageSizeUnsupported(contents.Span);
        await InvokeAsync("importDatabase", cancellationToken, databaseName, contents.ToArray())
            .ConfigureAwait(false);
    }

    void IBsqliteStorageImportCheck.ThrowIfCannotImport(ReadOnlySpan<byte> header) => ThrowIfPageSizeUnsupported(header);

    /// <summary>
    /// The VFS keeps a database in blocks of 4096 bytes and finds the one holding an offset by the nearest
    /// block start at or below it, while SQLite writes whole pages at the database's own page size. An image
    /// of 1024-byte pages imported as 4096-byte blocks reads fine until the first write adds a 1024-byte block
    /// inside one of them: the read of the next page then ends at that block and comes back zero-filled, and
    /// the database reports itself malformed. The page size of a database on this backend cannot be changed
    /// either (<c>PRAGMA page_size</c> is refused), so such an image is refused before anything is written.
    /// </summary>
    internal static void ThrowIfPageSizeUnsupported(ReadOnlySpan<byte> contents)
    {
        var pageSize = Bit.Bsqlite.BsqliteDatabaseImage.PageSize(contents);
        if (pageSize is not (0 or BsqliteIndexedDbStorageProvider.PageSize))
        {
            throw new NotSupportedException(
                $"The database has {pageSize}-byte pages, and the indexeddb backend keeps only databases of "
                + $"{BsqliteIndexedDbStorageProvider.PageSize}-byte pages. Change its page size first - "
                + $"PRAGMA page_size = {BsqliteIndexedDbStorageProvider.PageSize}; VACUUM; where it is kept now - "
                + "or keep it on a backend that takes it, such as opfs.");
        }
    }

    private async ValueTask<T> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        string identifier,
        CancellationToken cancellationToken,
        params object?[] args)
    {
        var module = await ImportAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await module.InvokeAsync<T>(identifier, cancellationToken, args).ConfigureAwait(false);
        }
        finally
        {
            await module.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async ValueTask InvokeAsync(
        string identifier,
        CancellationToken cancellationToken,
        params object?[] args)
    {
        var module = await ImportAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await module.InvokeVoidAsync(identifier, cancellationToken, args).ConfigureAwait(false);
        }
        finally
        {
            await module.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async ValueTask<IJSObjectReference> ImportAsync(CancellationToken cancellationToken)
    {
        if (js is null)
        {
            throw new InvalidOperationException(
                "IndexedDB admin operations need a JavaScript runtime. They are not available in this process.");
        }

        return await js
            .InvokeAsync<IJSObjectReference>(
                "import",
                cancellationToken,
                BsqliteIndexedDbStorageProvider.AdminModuleUrl)
            .ConfigureAwait(false);
    }
}
