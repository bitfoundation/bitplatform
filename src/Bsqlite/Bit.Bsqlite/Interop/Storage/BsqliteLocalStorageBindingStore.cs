using Microsoft.JSInterop;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Remembers sticky bindings in <c>localStorage</c> so they survive a reload.
/// </summary>
public sealed class BsqliteLocalStorageBindingStore(IJSRuntime js) : IBsqliteStorageBindingStore
{
    private const string Prefix = "bit-bsqlite.binding.";

    public async ValueTask<string?> GetProviderNameAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        return await js
            .InvokeAsync<string?>("localStorage.getItem", cancellationToken, Prefix + databaseName)
            .ConfigureAwait(false);
    }

    public async ValueTask SetProviderNameAsync(
        string databaseName,
        string providerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        await js
            .InvokeVoidAsync("localStorage.setItem", cancellationToken, Prefix + databaseName, providerName)
            .ConfigureAwait(false);
    }

    public async ValueTask RemoveAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        await js
            .InvokeVoidAsync("localStorage.removeItem", cancellationToken, Prefix + databaseName)
            .ConfigureAwait(false);
    }
}
