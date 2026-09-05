using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Device_Posture_API">Device Posture API</see>
/// (<c>navigator.devicePosture</c>): whether a foldable device is currently flat or folded across
/// its hinge.
/// </summary>
/// <remarks>
/// Chromium only. Every device that is not a foldable reports
/// <see cref="DevicePostureType.Continuous"/>, so a layout can branch on the posture without
/// special-casing the ordinary hardware.
/// <br/>
/// The posture says *that* the device is folded, not where the fold is - that comes from the CSS
/// viewport segments.
/// </remarks>
[ButilService(typeof(DevicePosture))]
public class DevicePosture(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeDevicePostureChange);

    private readonly ConcurrentDictionary<Guid, Action<DevicePostureType>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<DevicePosture>? _dotNetRef;
    private DotNetObjectReference<DevicePosture> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    private static DevicePostureType Parse(string? value)
        => value == "folded" ? DevicePostureType.Folded : DevicePostureType.Continuous;

    /// <summary>True when the runtime exposes <c>navigator.devicePosture</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.devicePosture.isSupported");

    /// <summary>
    /// The current posture. <see cref="DevicePostureType.Continuous"/> on an unsupported browser,
    /// which is the honest answer for hardware that cannot fold.
    /// </summary>
    public async ValueTask<DevicePostureType> GetPosture()
        => Parse(await js.Invoke<string>("BitButil.devicePosture.getPosture"));

    /// <summary>
    /// Invoked from JS on the posture <c>change</c> event. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeDevicePostureChange(Guid id, string type)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(Parse(type));
    }

    /// <summary>
    /// Runs <paramref name="handler"/> whenever the device is folded or unfolded.
    /// </summary>
    /// <returns>A subscription - dispose it to detach the listener.</returns>
    /// <exception cref="InvalidOperationException">
    /// The listener was not attached - the browser does not expose <c>navigator.devicePosture</c>.
    /// </exception>
    [DynamicDependency(nameof(InvokeDevicePostureChange), typeof(DevicePosture))]
    public async ValueTask<ButilSubscription> SubscribeChange(Action<DevicePostureType> handler)
    {
        // The detach goes straight to JS rather than through RemoveChange: the helper has already
        // taken the entry out by then, and RemoveChange returns early when it finds nothing to remove.
        return await ButilSubscriptionHelper.Register(_handlers, handler,
                                                      id => js.InvokeRegister("BitButil.devicePosture.addChange", DotNetRef, id),
                                                      id => js.InvokeVoid("BitButil.devicePosture.removeChange", new[] { id }),
                                                      "The device posture listener could not be attached - the API is not available.");
    }

    /// <summary>Detaches one posture listener by the id its subscription carries.</summary>
    public async ValueTask RemoveChange(Guid id)
    {
        if (_handlers.TryRemove(id, out _) is false) return;

        await js.InvokeVoid("BitButil.devicePosture.removeChange", new[] { id });
    }

    /// <summary>Detaches every posture listener registered through this instance.</summary>
    public async ValueTask RemoveAllChanges()
    {
        if (_handlers.IsEmpty) return;

        var ids = _handlers.Keys.ToArray();
        _handlers.Clear();

        await js.InvokeVoid("BitButil.devicePosture.removeChange", ids);
    }

    /// <summary>Detaches every posture listener this instance registered and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await RemoveAllChanges();
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
