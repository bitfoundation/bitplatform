using Bit.Blex.Query;
using Microsoft.JSInterop;

namespace Bit.Blex.Blazor;

/// <summary>
/// Tells the query cache when the browser tab becomes visible again or the network comes back, so stale
/// queries someone is watching refetch -- what <see cref="BlexQueryOptions.RefetchOnFocus"/> is for -- and, with
/// <see cref="BlexQueryClientOptions.SyncAcrossTabs"/>, carries key invalidations between the app's tabs.
/// Started by <see cref="BlexProvider"/> when a <see cref="BlexQueryClient"/> is registered.
/// </summary>
internal sealed class BlexQueryFocusWatcher(IJSRuntime js, BlexQueryClient client, BlexManager manager) : IAsyncDisposable
{
    /// <summary>The <c>BroadcastChannel</c> the app's tabs share invalidations on.</summary>
    internal const string ChannelName = "bit-blex-query";

    private IJSObjectReference? _module;
    private DotNetObjectReference<BlexQueryFocusWatcher>? _self;
    private int _handle;
    private Task? _starting;
    private bool _syncing;
    private bool _reportsOffline;
    private bool _disposed;

    /// <summary>Starts watching; calling it again returns the same start.</summary>
    /// <param name="reportOffline">Also report the network going away, which pauses requests: where they leave from this device.</param>
    public Task StartAsync(bool reportOffline = false)
    {
        _reportsOffline = reportOffline;
        return _starting ??= _disposed ? Task.CompletedTask : StartCoreAsync(reportOffline);
    }

    private async Task StartCoreAsync(bool reportOffline)
    {
        try
        {
            var sync = client.Options.SyncAcrossTabs;
            _module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Bit.Blex/bit-blex-query.js");
            _self = DotNetObjectReference.Create(this);
            _handle = await _module.InvokeAsync<int>("watch", _self, reportOffline, sync ? ChannelName : null);

            if (sync && !_disposed)
            {
                client.InvalidatedHere += OnInvalidatedHere;
                _syncing = true;
            }
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

    /// <summary>Invoked from JS when the browser went offline (only where requests leave from this device).</summary>
    [JSInvokable]
    public void OnOffline() => client.NotifyOffline();

    /// <summary>Invoked from JS when another tab invalidated a key (<see cref="BlexQueryClientOptions.SyncAcrossTabs"/>).</summary>
    [JSInvokable]
    public Task OnInvalidatedElsewhere(string key, bool exact) => client.InvalidateFromElsewhereAsync(key, exact);

    private void OnInvalidatedHere(string key, bool exact)
    {
        if (_module is not { } module || _handle <= 0 || _disposed)
            return;

        _ = PostAsync(module, key, exact);

        async Task PostAsync(IJSObjectReference module, string key, bool exact)
        {
            try
            {
                await module.InvokeVoidAsync("broadcastInvalidation", _handle, key, exact);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
            {
                // The page is going away; the other tabs refetch on their own focus.
            }
            catch (Exception ex)
            {
                manager.ReportError("query", ex, $"passing the invalidation of {key} to other tabs");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // A start still in flight finishes first, so what it attaches is released below rather than left behind.
        if (_starting is not null)
            await _starting;

        // The client may outlive the page (a singleton in Hybrid): it must not keep calling into a disposed module.
        if (_syncing)
        {
            client.InvalidatedHere -= OnInvalidatedHere;
            _syncing = false;
        }

        // Nobody is left to report the network coming back: leaving the cache offline would pause every request until
        // a watcher hears an "online" event that already happened. Like a fresh start, it assumes online.
        if (_reportsOffline && !client.IsOnline)
            client.NotifyReconnected();

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
