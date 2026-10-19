using Microsoft.JSInterop;

namespace Bit.Blex.Blazor;

/// <summary>
/// <see cref="IBlexStorage"/> backed by the browser's <c>localStorage</c>/<c>sessionStorage</c>
/// via a tiny JS bridge. Used by Blex persistence to survive page reloads in Blazor WebAssembly.
/// </summary>
public sealed class BlexBrowserStorage : IBlexStorage, IBlexExternalStorageChange, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly string _area;
    private IJSObjectReference? _module;
    private DotNetObjectReference<BlexBrowserStorage>? _self;

    /// <summary>
    /// Handle identifying this instance's <c>storage</c> listener inside the shared JS module
    /// (0 = none). The module is cached per URL, so every storage instance on the page shares it;
    /// without a handle, a second instance's <c>unwatch</c> would detach whichever listener happened
    /// to be registered last.
    /// </summary>
    private int _watchHandle;

    private bool _watching;

    /// <summary>Creates a storage instance over the given Web Storage area.</summary>
    public BlexBrowserStorage(IJSRuntime js, BlexBrowserStorageKind kind = BlexBrowserStorageKind.Local)
    {
        _js = js;
        _area = kind == BlexBrowserStorageKind.Session ? "session" : "local";
    }

    /// <inheritdoc />
    /// <remarks>
    /// Backed by the browser's <c>storage</c> event, which fires only in the tabs that did
    /// <em>not</em> write the value -- so this never echoes back this instance's own writes.
    /// </remarks>
    public event Action<string?>? ExternalChange;

    /// <inheritdoc />
    /// <remarks>
    /// A no-op for <see cref="BlexBrowserStorageKind.Session"/>: <c>sessionStorage</c> is per-tab,
    /// so there is no other instance to hear from.
    /// </remarks>
    public async ValueTask StartWatchingAsync(CancellationToken cancellationToken = default)
    {
        if (_watching || _area != "local")
            return;

        var module = await ModuleAsync(cancellationToken);
        _self ??= DotNetObjectReference.Create(this);
        _watchHandle = await module.InvokeAsync<int>("watch", cancellationToken, _area, _self);
        _watching = _watchHandle > 0;
    }

    /// <summary>Invoked from JS when another tab wrote to this origin's storage.</summary>
    /// <param name="key">The changed key, or <c>null</c> when the area was cleared wholesale.</param>
    [JSInvokable]
    public Task OnExternalChangeAsync(string? key)
    {
        ExternalChange?.Invoke(key);
        return Task.CompletedTask;
    }

    private async ValueTask<IJSObjectReference> ModuleAsync(CancellationToken ct)
        => _module ??= await _js.InvokeAsync<IJSObjectReference>(
            "import", ct, "./_content/Bit.Blex.Blazor/bit-blex-storage.js");

    /// <inheritdoc />
    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken);
        return await module.InvokeAsync<string?>("get", cancellationToken, _area, key);
    }

    /// <inheritdoc />
    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("set", cancellationToken, _area, key, value);
    }

    /// <inheritdoc />
    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("remove", cancellationToken, _area, key);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                // Detach the listener before dropping the module: it holds the DotNetObjectReference
                // below, and a live listener calling into a disposed reference throws on every
                // storage write in every other tab.
                if (_watching)
                {
                    await _module.InvokeVoidAsync("unwatch", _watchHandle);
                    _watching = false;
                    _watchHandle = 0;
                }

                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or TaskCanceledException or ObjectDisposedException)
        {
            // The page that held the module is gone, so there is nothing left to clean up. A circuit that
            // ended reports that as JSDisconnectedException; a BlazorWebView whose page was reloaded
            // disposes the old page's scope after the new page has attached, and the new page answers the
            // unwatch with a JSException for a module it never had. Disposal is best-effort either way, and
            // an exception escaping it there would take down the whole hybrid app.
        }
        finally
        {
            _self?.Dispose();
            _self = null;
        }
    }
}
