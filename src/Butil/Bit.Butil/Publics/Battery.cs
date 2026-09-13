using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/BatteryManager">Battery Status API</see>.
/// </summary>
/// <remarks>
/// Browser support is uneven (Firefox/Safari intentionally don't expose this). When unsupported,
/// <see cref="IsSupported"/> returns false and <see cref="GetStatus"/> reports a charged-AC-power
/// stub so callers don't have to special-case missing data.
/// </remarks>
[ButilService(typeof(Battery))]
public class Battery(IJSRuntime js) : IAsyncDisposable
{
    internal const string ChangeMethodName = nameof(InvokeBatteryChange);

    private readonly ConcurrentDictionary<Guid, Action<BatteryStatus>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Battery>? _dotNetRef;
    private DotNetObjectReference<Battery> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.getBattery</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.battery.isSupported");

    /// <summary>One-shot snapshot of the battery state.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BatteryStatus))]
    public ValueTask<BatteryStatus> GetStatus() => js.Invoke<BatteryStatus>("BitButil.battery.getStatus");

    /// <summary>Invoked from JS on each battery change. See <see cref="SubscribeChange"/>.</summary>
    [JSInvokable(ChangeMethodName)]
    public void InvokeBatteryChange(Guid id, BatteryStatus status)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(status);
    }

    /// <summary>
    /// Watches the battery and calls <paramref name="handler"/> with a fresh snapshot whenever it
    /// changes - plugged in, unplugged, or the level moved.
    /// </summary>
    /// <param name="handler">Called with the new state on every change.</param>
    /// <returns>
    /// A subscription to dispose, or null on a browser without the Battery Status API - there is
    /// nothing to watch there, and <see cref="GetStatus"/>'s AC-power stub never changes.
    /// </returns>
    /// <remarks>
    /// The spec has no single "change" event; this attaches to all four
    /// (<c>chargingchange</c>, <c>levelchange</c>, <c>chargingtimechange</c>,
    /// <c>dischargingtimechange</c>) and reports the whole state each time, so a handler never has
    /// to work out which field moved.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BatteryStatus))]
    [DynamicDependency(nameof(InvokeBatteryChange), typeof(Battery))]
    public async ValueTask<ButilSubscription?> SubscribeChange(Action<BatteryStatus> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;

        if (await js.Invoke<bool>("BitButil.battery.subscribe", DotNetRef, id) is false)
        {
            _handlers.TryRemove(id, out _);
            return null;
        }

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.battery.unsubscribe", id);
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
            await js.InvokeVoid("BitButil.battery.disposeAll");
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
