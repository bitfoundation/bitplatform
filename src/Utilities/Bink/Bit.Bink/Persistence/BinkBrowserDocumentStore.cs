using Microsoft.JSInterop;

namespace Bit.Bink;

/// <summary>
/// A store in the browser's IndexedDB, in a database of its own named <c>bit-bink</c>. It survives reloading the
/// page, closing the tab and a browser crash, which is what lets <see cref="BinkAutosave"/> recover a drawing, and it
/// holds far more than <c>localStorage</c> would. It stays on the user's device: nothing is sent anywhere.
/// </summary>
/// <remarks>
/// <para>
/// It works in every hosting model through <see cref="IJSRuntime"/>. A stored document comes back to .NET as a
/// stream, so loading a large one does not run into Blazor Server's 32 KB limit on a message from the browser.
/// </para>
/// <para>
/// Storage is per origin and per browser profile, and a browser may clear it under storage pressure or in a private
/// window. Treat it as a recovery copy, not as the only place a drawing lives. Calls fail with
/// <see cref="JSException"/> where IndexedDB is unavailable, and with <see cref="InvalidOperationException"/> while
/// prerendering, when there is no browser yet.
/// </para>
/// </remarks>
public sealed class BinkBrowserDocumentStore : IBinkDocumentStore, IAsyncDisposable
{
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    private readonly IJSRuntime _jsRuntime;
    private Task<IJSObjectReference>? _module;

    /// <summary>Creates a store over the browser's IndexedDB.</summary>
    /// <param name="jsRuntime">The JavaScript runtime, typically injected into the component that creates the store.</param>
    public BinkBrowserDocumentStore(IJSRuntime jsRuntime)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        _jsRuntime = jsRuntime;
    }

    /// <summary>The largest document read back, in bytes. A stored document is data another tab could have written.</summary>
    public long MaxDocumentBytes { get; init; } = 256L * 1024 * 1024;

    /// <inheritdoc />
    public async Task SaveAsync(string key, ReadOnlyMemory<byte> document, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        IJSObjectReference module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        await module.InvokeVoidAsync("storeSave", cancellationToken, key, document.ToArray()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<byte[]?> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        IJSObjectReference module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        // The stream leads with a byte saying whether anything is stored: Blazor can make a stream of neither null nor
        // an empty blob, so "nothing" has to be a byte of its own.
        IJSStreamReference stream = await module.InvokeAsync<IJSStreamReference>("storeLoad", cancellationToken, key).ConfigureAwait(false);
        byte[] bytes = await BinkInterop.ReadBytesAsync(stream, MaxDocumentBytes + 1, cancellationToken).ConfigureAwait(false);
        return bytes.Length > 1 && bytes[0] == 1 ? bytes[1..] : null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        IJSObjectReference module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        return await module.InvokeAsync<bool>("storeDelete", cancellationToken, key).ConfigureAwait(false);
    }

    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        Task<IJSObjectReference> module = _module ??= _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath).AsTask();
        try
        {
            return await module.ConfigureAwait(false);
        }
        catch
        {
            // A failed import (prerendering, a blocked script) is not remembered: the next call tries again.
            if (ReferenceEquals(_module, module)) _module = null;
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Task<IJSObjectReference>? module = _module;
        _module = null;
        if (module is null || !module.IsCompletedSuccessfully) return;
        try
        {
            await module.Result.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
