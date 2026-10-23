using Bit.Blex.Query;
using Microsoft.JSInterop;

namespace Bit.Blex.Blazor;

/// <summary>
/// Tells the query cache when the browser tab becomes visible again or the network comes back, so stale
/// queries someone is watching refetch -- what <see cref="BlexQueryOptions.RefetchOnFocus"/> is for.
/// Started by <see cref="BlexProvider"/> when a <see cref="BlexQueryClient"/> is registered.
/// </summary>
internal sealed class BlexQueryFocusWatcher(IJSRuntime js, BlexQueryClient client, BlexManager manager) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<BlexQueryFocusWatcher>? _self;
    private int _handle;
    private Task? _starting;
    private bool _disposed;

    /// <summary>Starts watching; calling it again returns the same start.</summary>
    public Task StartAsync() => _starting ??= _disposed ? Task.CompletedTask : StartCoreAsync();

    private async Task StartCoreAsync()
    {
        try
        {
            _module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Bit.Blex/bit-blex-query.js");
            _self = DotNetObjectReference.Create(this);
            _handle = await _module.InvokeAsync<int>("watch", _self);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
        {
            // The page went away while starting; there is nothing to watch.
        }
        catch (Exception ex)
        {
            // Queries still work without it; they just do not refetch on focus.
            manager.ReportError("query", ex, "watching focus and network");
        }
    }

    /// <summary>Invoked from JS when the page became visible again.</summary>
    [JSInvokable]
    public void OnFocused() => client.NotifyFocused();

    /// <summary>Invoked from JS when the browser came back online.</summary>
    [JSInvokable]
    public void OnReconnected() => client.NotifyReconnected();

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // A start still in flight finishes first, so what it attaches is released below rather than left behind.
        if (_starting is not null)
            await _starting;

        try
        {
            if (_module is not null)
            {
                // Detach the listeners first: they hold the reference below.
                if (_handle > 0)
                    await _module.InvokeVoidAsync("unwatch", _handle);

                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or TaskCanceledException or ObjectDisposedException)
        {
            // The page is gone (a circuit that ended, a reloaded BlazorWebView); disposal is best-effort.
        }
        finally
        {
            _module = null;
            _handle = 0;
            _self?.Dispose();
            _self = null;
        }
    }
}
