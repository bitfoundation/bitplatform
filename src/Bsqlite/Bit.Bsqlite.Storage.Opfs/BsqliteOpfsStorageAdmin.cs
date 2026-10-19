using System.Diagnostics.CodeAnalysis;
using Bit.Bsqlite.Storage;
using Microsoft.JSInterop;

namespace Bit.Bsqlite.Storage.Opfs;

/// <summary>
/// Database-level operations against OPFS, used by diagnostics and cross-provider migration.
/// </summary>
/// <remarks>
/// Talks to <c>bit-bsqlite-opfs-admin.js</c>. Related files (<c>-journal</c>, <c>-wal</c>) are not databases
/// and are stripped on list, delete, and import.
/// </remarks>
internal sealed class BsqliteOpfsStorageAdmin(IJSRuntime? js) : IBsqliteStorageAdmin
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
                "OPFS admin operations need a JavaScript runtime. They are not available in this process.");
        }

        return await js
            .InvokeAsync<IJSObjectReference>("import", cancellationToken, BsqliteOpfsStorageProvider.AdminModuleUrl)
            .ConfigureAwait(false);
    }
}
