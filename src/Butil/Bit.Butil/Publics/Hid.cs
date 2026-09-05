using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebHID_API">WebHID API</see>
/// (<c>navigator.hid</c>): talk to a human-interface device the operating system's own drivers do
/// not fully expose - a macro pad, a flight yoke, a vendor-defined control surface.
/// </summary>
/// <remarks>
/// Chromium only, and only over HTTPS. <see cref="RequestDevice"/> opens the browser's device
/// chooser and so must run inside a user gesture.
/// <br/>
/// The browser blocks devices whose usage pages would let a page impersonate the user's own input -
/// keyboards and mice among them - so WebHID is for the long tail, not for reading the keyboard.
/// </remarks>
[ButilService(typeof(Hid))]
public class Hid(IJSRuntime js) : IAsyncDisposable
{
    internal const string InputReportMethodName = nameof(InvokeHidInputReport);
    internal const string ConnectedMethodName = nameof(InvokeHidConnected);
    internal const string DisconnectedMethodName = nameof(InvokeHidDisconnected);

    // Keyed by subscription id, but carrying the device each one belongs to: hid.ts release() detaches
    // that device's listeners, so the .NET closures behind them have to go at the same moment -
    // otherwise disposing one device leaks its handlers for as long as the service lives.
    private readonly ConcurrentDictionary<Guid, (string DeviceId, Action<HidInputReport> Handler)> _inputHandlers = new();
    private readonly ConcurrentDictionary<Guid, (Action<HidDevice>? OnConnected, Action<HidDevice>? OnDisconnected)> _connectionHandlers = new();

    // Every handle this service handed out, so a scope/circuit teardown closes the device even
    // when the caller never disposed it.
    private readonly ConcurrentDictionary<string, HidDevice> _devices = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Hid>? _dotNetRef;
    private DotNetObjectReference<Hid> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.hid</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.hid.isSupported");

    /// <summary>
    /// Opens the browser's device chooser and returns the devices the user picked - an array,
    /// because one physical device can present as several. Empty when they dismissed it. Must be
    /// called from a user gesture.
    /// </summary>
    /// <param name="filters">Which devices the chooser lists. Pass none to list everything the browser allows.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidDeviceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidCollectionInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidReportInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidDeviceFilter))]
    public async ValueTask<HidDevice[]> RequestDevice(params HidDeviceFilter[] filters)
    {
        var infos = await js.Invoke<HidDeviceInfo[]>("BitButil.hid.requestDevice", (object)filters);
        return [.. infos.Select(Track)];
    }

    /// <summary>The devices this origin has already been granted, without showing a chooser.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidDeviceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidCollectionInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidReportInfo))]
    public async ValueTask<HidDevice[]> GetDevices()
    {
        var infos = await js.Invoke<HidDeviceInfo[]>("BitButil.hid.getDevices");
        return [.. infos.Select(Track)];
    }

    // One handle per browser-side id. hid.ts idOf() deliberately hands the same id back for the same
    // device, so a device surfaced again by GetDevices or by a connect event has to return the handle
    // already in play: a second wrapper over one JS registry entry would let disposing either of them
    // release the device out from under the other. Info stays the snapshot the first handle was
    // created from - it is documented as not updating on its own, and GetInfo() re-reads it.
    private HidDevice Track(HidDeviceInfo info) => _devices.GetOrAdd(info.Id, _ => new HidDevice(this, js, info));

    // Called by a handle that is disposing itself, so the service stops holding it. The JS-side
    // release() detaches every input listener attached through that handle, so the .NET closures
    // behind them have to go with it - otherwise disposing one device leaks the handlers of that
    // device for as long as the service lives. The registry removal is identity-checked so a handle
    // that has already been replaced cannot untrack its successor.
    internal void Forget(HidDevice device)
    {
        _devices.TryRemove(new KeyValuePair<string, HidDevice>(device.Id, device));

        foreach (var pair in _inputHandlers)
        {
            if (pair.Value.DeviceId == device.Id) _inputHandlers.TryRemove(pair.Key, out _);
        }
    }

    /// <summary>
    /// Invoked from JS for each input report. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InputReportMethodName)]
    public void InvokeHidInputReport(Guid id, HidInputReport report)
    {
        if (_inputHandlers.TryGetValue(id, out var entry)) entry.Handler.Invoke(report);
    }

    /// <summary>Invoked from JS on <c>navigator.hid</c>'s <c>connect</c>. See <see cref="InvokeHidInputReport"/>.</summary>
    [JSInvokable(ConnectedMethodName)]
    public void InvokeHidConnected(Guid id, HidDeviceInfo info)
    {
        if (_connectionHandlers.TryGetValue(id, out var handlers)) handlers.OnConnected?.Invoke(Track(info));
    }

    /// <summary>Invoked from JS on <c>disconnect</c>. See <see cref="InvokeHidInputReport"/>.</summary>
    [JSInvokable(DisconnectedMethodName)]
    public void InvokeHidDisconnected(Guid id, HidDeviceInfo info)
    {
        if (_connectionHandlers.TryGetValue(id, out var handlers)) handlers.OnDisconnected?.Invoke(Track(info));
    }

    [DynamicDependency(nameof(InvokeHidInputReport), typeof(Hid))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidInputReport))]
    internal async ValueTask<ButilSubscription> SubscribeInputReports(string deviceId, Action<HidInputReport> handler)
    {
        return await ButilSubscriptionHelper.Register(_inputHandlers, (deviceId, handler),
                                                      id => js.InvokeRegister("BitButil.hid.subscribeInputReports", id, DotNetRef, deviceId),
                                                      id => js.InvokeVoid("BitButil.hid.unsubscribeInputReports", id),
                                                      "The input report listener could not be attached - the device handle is no longer known.");
    }

    /// <summary>
    /// Watches devices being plugged in and unplugged. Only devices this origin already has
    /// permission for raise these.
    /// </summary>
    /// <returns>A subscription - dispose it to detach the listeners.</returns>
    /// <exception cref="InvalidOperationException">The listeners were not attached - no WebHID support.</exception>
    [DynamicDependency(nameof(InvokeHidConnected), typeof(Hid))]
    [DynamicDependency(nameof(InvokeHidDisconnected), typeof(Hid))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidDeviceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidCollectionInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidReportInfo))]
    public async ValueTask<ButilSubscription> SubscribeConnection(Action<HidDevice>? onConnected = null,
                                                                  Action<HidDevice>? onDisconnected = null)
    {
        return await ButilSubscriptionHelper.Register(_connectionHandlers, (onConnected, onDisconnected),
                                                      id => js.InvokeRegister("BitButil.hid.subscribeConnection", id, DotNetRef),
                                                      id => js.InvokeVoid("BitButil.hid.unsubscribeConnection", id),
                                                      "The HID connection listeners could not be attached - the WebHID API is not available.");
    }

    /// <summary>
    /// Closes every device this service handed out, detaches its listeners and releases the
    /// interop reference.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            // Releasing a device detaches its input listeners, and every entry left here belongs to a
            // device still in _devices - Forget() strips the rest - so the loop below covers them.
            _inputHandlers.Clear();

            foreach (var id in _connectionHandlers.Keys.ToArray())
            {
                _connectionHandlers.TryRemove(id, out _);
                await js.InvokeVoid("BitButil.hid.unsubscribeConnection", id);
            }

            foreach (var device in _devices.Values.ToArray())
            {
                await device.DisposeAsync();
            }
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
