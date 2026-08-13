using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/NetworkInformation">Network Information API</see>
/// (<c>navigator.connection</c>) plus the always-available <c>navigator.onLine</c>.
/// </summary>
public class NetworkInformation(IJSRuntime js) : IAsyncDisposable
{
    internal const string ChangeMethodName = nameof(InvokeNetworkChange);

    private readonly ConcurrentDictionary<Guid, Action<NetworkConnectionStatus>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<NetworkInformation>? _dotNetRef;
    private DotNetObjectReference<NetworkInformation> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.connection</c>.</summary>
    /// <remarks>
    /// This is the Chromium-only half of the API. <see cref="GetStatus"/> works regardless - on a
    /// browser that answers <c>false</c> here it still reports
    /// <see cref="NetworkConnectionStatus.Online"/> from <c>navigator.onLine</c> and leaves the
    /// connection-quality fields null - so treat this as "is the detail available", not as
    /// "may I call GetStatus".
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.networkInformation.isSupported");

    /// <summary>One-shot snapshot of the network state.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NetworkConnectionStatus))]
    public ValueTask<NetworkConnectionStatus> GetStatus()
        => js.Invoke<NetworkConnectionStatus>("BitButil.networkInformation.getStatus");

    /// <summary>Invoked from JS on each connectivity change. See <see cref="SubscribeChange"/>.</summary>
    [JSInvokable(ChangeMethodName)]
    public void InvokeNetworkChange(Guid id, NetworkConnectionStatus status)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(status);
    }

    /// <summary>
    /// Watches connectivity and calls <paramref name="handler"/> with a fresh snapshot whenever it
    /// changes - going offline or online, or (on Chromium) the connection quality shifting.
    /// </summary>
    /// <param name="handler">Called with the new state on every change.</param>
    /// <returns>A subscription - dispose it to detach the listeners.</returns>
    /// <remarks>
    /// Attaches to the window's <c>online</c>/<c>offline</c> events as well as
    /// <c>navigator.connection</c>'s <c>change</c>, so this is useful on every browser - it just
    /// won't report quality changes where <see cref="IsSupported"/> is false.
    /// <br/>
    /// Going "online" only means the device has a network attached, not that anything is reachable;
    /// a captive portal still reports online. Treat it as a hint to retry, not a guarantee.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NetworkConnectionStatus))]
    [DynamicDependency(nameof(InvokeNetworkChange), typeof(NetworkInformation))]
    public async ValueTask<ButilSubscription> SubscribeChange(Action<NetworkConnectionStatus> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;
        await js.InvokeVoid("BitButil.networkInformation.subscribe", DotNetRef, id);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.networkInformation.unsubscribe", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, detaches any listener whose <see cref="ButilSubscription"/> was
    /// never disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.networkInformation.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
