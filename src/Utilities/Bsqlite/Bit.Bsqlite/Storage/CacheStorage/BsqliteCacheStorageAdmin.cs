using System.Diagnostics.CodeAnalysis;
using Bit.Bsqlite.Storage;
using Microsoft.JSInterop;

namespace Bit.Bsqlite.Storage.CacheStorage;

internal sealed class BsqliteCacheStorageAdmin(IJSRuntime? js) : IBsqliteStorageAdmin
{
    public async ValueTask<bool> ExistsAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        return await InvokeAsync<bool>("exists", cancellationToken, databaseName).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
        => await InvokeAsync<string[]>("list", cancellationToken).ConfigureAwait(false);

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
        return await InvokeAsync<byte[]>("exportDatabase", cancellationToken, databaseName)
            .ConfigureAwait(false);
    }

    public async ValueTask ImportAsync(
        string databaseName,
        ReadOnlyMemory<byte> contents,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        await InvokeAsync("importDatabase", cancellationToken, databaseName, contents.ToArray())
            .ConfigureAwait(false);
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
                "Cache Storage admin operations need a JavaScript runtime.");
        }

        return await js
            .InvokeAsync<IJSObjectReference>(
                "import",
                cancellationToken,
                BsqliteCacheStorageProvider.AdminModuleUrl)
            .ConfigureAwait(false);
    }
}
