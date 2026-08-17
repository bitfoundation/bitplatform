using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Device_orientation_events">device orientation events</see>:
/// how the device is tilted (<c>deviceorientation</c>) and how it is being moved
/// (<c>devicemotion</c>).
/// </summary>
/// <remarks>
/// Only devices with the matching sensors fire these events, so on a desktop machine a
/// subscription usually stays silent - <see cref="IsOrientationSupported"/> tells you the events
/// exist, not that anything will arrive.
/// <br/>
/// On iOS the streams are gated: call <see cref="RequestPermission"/> from a click handler before
/// subscribing. Elsewhere there is no gate and it returns <see cref="DeviceSensorPermission.Granted"/>
/// straight away - see <see cref="NeedsPermission"/>.
/// </remarks>
[ButilService(typeof(DeviceOrientation))]
public class DeviceOrientation(IJSRuntime js) : IAsyncDisposable
{
    internal const string OrientationMethodName = nameof(InvokeOrientation);
    internal const string MotionMethodName = nameof(InvokeMotion);

    private readonly ConcurrentDictionary<Guid, Action<DeviceOrientationReading>> _orientationHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action<DeviceMotionReading>> _motionHandlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<DeviceOrientation>? _dotNetRef;
    private DotNetObjectReference<DeviceOrientation> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// True when the runtime exposes either <c>DeviceOrientationEvent</c> or
    /// <c>DeviceMotionEvent</c>.
    /// </summary>
    /// <remarks>
    /// The uniform spelling used by every other Butil class - "is any of this here at all".
    /// <see cref="IsOrientationSupported"/> and <see cref="IsMotionSupported"/> answer the finer
    /// question of which of the two streams exists.
    /// <br/>
    /// Same caveat as those two: a desktop browser reports true and then never fires anything,
    /// because the machine has no sensor.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.deviceOrientation.isSupported");

    /// <summary>True when the runtime exposes <c>DeviceOrientationEvent</c>.</summary>
    /// <remarks>
    /// A desktop browser reports true here and then never fires the event, because the machine has
    /// no orientation sensor. Treat it as "the API exists", not as "readings are coming".
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsOrientationSupported() => js.Invoke<bool>("BitButil.deviceOrientation.isOrientationSupported");

    /// <summary>True when the runtime exposes <c>DeviceMotionEvent</c>.</summary>
    /// <remarks>
    /// Same caveat as <see cref="IsOrientationSupported"/>: the class existing doesn't mean the
    /// hardware does.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsMotionSupported() => js.Invoke<bool>("BitButil.deviceOrientation.isMotionSupported");

    /// <summary>
    /// True on the engines that gate these events behind an explicit grant - in practice Safari on
    /// iOS. When false, <see cref="RequestPermission"/> is unnecessary but still safe to call.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> NeedsPermission() => js.Invoke<bool>("BitButil.deviceOrientation.needsPermission");

    /// <summary>
    /// Asks for access to the orientation and motion streams.
    /// </summary>
    /// <returns>
    /// <see cref="DeviceSensorPermission.Granted"/> on engines with no gate, otherwise the user's
    /// answer. A call made outside a user gesture is reported as
    /// <see cref="DeviceSensorPermission.Denied"/> rather than throwing.
    /// </returns>
    /// <remarks>
    /// Must be called from a user-gesture handler such as a click. The grant is not persisted
    /// across page loads, so ask again after a reload.
    /// </remarks>
    public async ValueTask<DeviceSensorPermission> RequestPermission()
    {
        var result = await js.Invoke<string>("BitButil.deviceOrientation.requestPermission");
        return result switch
        {
            "granted" => DeviceSensorPermission.Granted,
            "denied" => DeviceSensorPermission.Denied,
            // Prerender returns "" through the safe-default path; nothing was asked, so say so.
            _ => DeviceSensorPermission.Unknown,
        };
    }

    /// <summary>
    /// Invoked from JS for each orientation reading. Public + <see cref="JSInvokableAttribute"/> so
    /// it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(OrientationMethodName)]
    public void InvokeOrientation(Guid id, DeviceOrientationReading reading)
    {
        if (_orientationHandlers.TryGetValue(id, out var handler)) handler.Invoke(reading);
    }

    /// <summary>
    /// Invoked from JS for each motion reading. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MotionMethodName)]
    public void InvokeMotion(Guid id, DeviceMotionReading reading)
    {
        if (_motionHandlers.TryGetValue(id, out var handler)) handler.Invoke(reading);
    }

    /// <summary>
    /// Subscribes to tilt readings.
    /// </summary>
    /// <param name="handler">Called with each throttled reading.</param>
    /// <param name="absolute">
    /// When true, prefers <c>deviceorientationabsolute</c>, whose alpha is a compass heading
    /// relative to the earth rather than to wherever the device happened to be pointing when the
    /// listener attached. Falls back to the relative event where that isn't implemented, in which
    /// case <see cref="DeviceOrientationReading.Absolute"/> tells you what you actually got.
    /// </param>
    /// <param name="minIntervalMs">
    /// The floor between two callbacks. These events fire tens of times a second, which no Blazor
    /// render loop needs; the default keeps the interop channel usable. Pass 0 for every event.
    /// </param>
    /// <returns>A subscription - dispose it to detach the listener.</returns>
    [DynamicDependency(nameof(InvokeOrientation), typeof(DeviceOrientation))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DeviceOrientationReading))]
    public async ValueTask<ButilSubscription> SubscribeOrientation(Action<DeviceOrientationReading> handler,
                                                                    bool absolute = false,
                                                                    int minIntervalMs = 100)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _orientationHandlers[id] = handler;
        await js.InvokeVoid("BitButil.deviceOrientation.subscribeOrientation", DotNetRef, id, absolute, Math.Max(0, minIntervalMs));

        return new ButilSubscription(id, async () =>
        {
            _orientationHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.deviceOrientation.unsubscribe", id);
        });
    }

    /// <summary>
    /// Subscribes to acceleration and rotation-rate readings.
    /// </summary>
    /// <param name="handler">Called with each throttled reading.</param>
    /// <param name="minIntervalMs">
    /// The floor between two callbacks. Pass 0 for every event - see the note on
    /// <see cref="SubscribeOrientation"/>.
    /// </param>
    /// <returns>A subscription - dispose it to detach the listener.</returns>
    [DynamicDependency(nameof(InvokeMotion), typeof(DeviceOrientation))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DeviceMotionReading))]
    public async ValueTask<ButilSubscription> SubscribeMotion(Action<DeviceMotionReading> handler,
                                                               int minIntervalMs = 100)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _motionHandlers[id] = handler;
        await js.InvokeVoid("BitButil.deviceOrientation.subscribeMotion", DotNetRef, id, Math.Max(0, minIntervalMs));

        return new ButilSubscription(id, async () =>
        {
            _motionHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.deviceOrientation.unsubscribe", id);
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
            _orientationHandlers.Clear();
            _motionHandlers.Clear();
            await js.InvokeVoid("BitButil.deviceOrientation.disposeAll");
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
