using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebUSB_API">WebUSB API</see>
/// (<c>navigator.usb</c>): pick a USB device, claim one of its interfaces, and run control, bulk or
/// interrupt transfers against it.
/// </summary>
/// <remarks>
/// Chromium only, and only over HTTPS. <see cref="RequestDevice"/> opens the browser's device
/// chooser and so must run inside a user gesture.
/// <br/>
/// The operating system keeps its own claim on devices it has a driver for - keyboards, mice, mass
/// storage, most webcams - and the browser blocks those outright. WebUSB is for devices no driver
/// has taken: microcontroller boards, instruments, programmers.
/// </remarks>
[ButilService(typeof(Usb))]
public class Usb(IJSRuntime js) : IAsyncDisposable
{
    internal const string ConnectedMethodName = nameof(InvokeUsbConnected);
    internal const string DisconnectedMethodName = nameof(InvokeUsbDisconnected);

    private readonly ConcurrentDictionary<Guid, (Action<UsbDevice>? OnConnected, Action<UsbDevice>? OnDisconnected)> _connectionHandlers = new();

    // Every handle this service handed out, so a scope/circuit teardown closes the device even
    // when the caller never disposed it.
    private readonly ConcurrentDictionary<string, UsbDevice> _devices = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Usb>? _dotNetRef;
    private DotNetObjectReference<Usb> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.usb</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.usb.isSupported");

    /// <summary>
    /// Opens the browser's device chooser and returns the device the user picked, or null when
    /// they dismissed it. Must be called from a user gesture.
    /// </summary>
    /// <param name="filters">
    /// Which devices the chooser lists. Pass none to list every device the browser is willing to
    /// show - which on a typical machine is a short list, since drivers have claimed the rest.
    /// </param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbDeviceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbConfigurationInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbInterfaceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbAlternateInterfaceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbEndpointInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbDeviceFilter))]
    public async ValueTask<UsbDevice?> RequestDevice(params UsbDeviceFilter[] filters)
    {
        var info = await js.Invoke<UsbDeviceInfo?>("BitButil.usb.requestDevice", (object)filters);
        return info is null ? null : Track(info);
    }

    /// <summary>The devices this origin has already been granted, without showing a chooser.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbDeviceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbConfigurationInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbInterfaceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbAlternateInterfaceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbEndpointInfo))]
    public async ValueTask<UsbDevice[]> GetDevices()
    {
        var infos = await js.Invoke<UsbDeviceInfo[]>("BitButil.usb.getDevices");
        return [.. infos.Select(Track)];
    }

    // One handle per browser-side id. usb.ts idOf() deliberately hands the same id back for the same
    // device, so a device surfaced again by GetDevices or by a connect event has to return the handle
    // already in play: a second wrapper over one JS registry entry would let disposing either of them
    // release the device out from under the other. Info stays the snapshot the first handle was
    // created from - it is documented as not updating on its own, and GetInfo() re-reads it.
    private UsbDevice Track(UsbDeviceInfo info) => _devices.GetOrAdd(info.Id, _ => new UsbDevice(this, js, info));

    // Called by a handle that is disposing itself, so the service stops holding it. The removal is
    // identity-checked so a handle that has already been replaced cannot untrack its successor.
    internal void Forget(UsbDevice device) => _devices.TryRemove(new KeyValuePair<string, UsbDevice>(device.Id, device));

    /// <summary>
    /// Invoked from JS on <c>navigator.usb</c>'s <c>connect</c>. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ConnectedMethodName)]
    public void InvokeUsbConnected(Guid id, UsbDeviceInfo info)
    {
        if (_connectionHandlers.TryGetValue(id, out var handlers)) handlers.OnConnected?.Invoke(Track(info));
    }

    /// <summary>Invoked from JS on <c>disconnect</c>. See <see cref="InvokeUsbConnected"/>.</summary>
    [JSInvokable(DisconnectedMethodName)]
    public void InvokeUsbDisconnected(Guid id, UsbDeviceInfo info)
    {
        if (_connectionHandlers.TryGetValue(id, out var handlers)) handlers.OnDisconnected?.Invoke(Track(info));
    }

    /// <summary>
    /// Watches devices being plugged in and unplugged. Only devices this origin already has
    /// permission for raise these - plugging in a stranger's device is not something the page is
    /// told about.
    /// </summary>
    /// <returns>A subscription - dispose it to detach the listeners.</returns>
    /// <exception cref="InvalidOperationException">The listeners were not attached - no WebUSB support.</exception>
    [DynamicDependency(nameof(InvokeUsbConnected), typeof(Usb))]
    [DynamicDependency(nameof(InvokeUsbDisconnected), typeof(Usb))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbDeviceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbConfigurationInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbInterfaceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbAlternateInterfaceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbEndpointInfo))]
    public async ValueTask<ButilSubscription> SubscribeConnection(Action<UsbDevice>? onConnected = null,
                                                                  Action<UsbDevice>? onDisconnected = null)
    {
        return await ButilSubscriptionHelper.Register(_connectionHandlers, (onConnected, onDisconnected),
                                                      id => js.InvokeRegister("BitButil.usb.subscribeConnection", id, DotNetRef),
                                                      id => js.InvokeVoid("BitButil.usb.unsubscribeConnection", id),
                                                      "The USB connection listeners could not be attached - the WebUSB API is not available.");
    }

    /// <summary>
    /// Closes every device this service handed out, detaches its listeners and releases the
    /// interop reference.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            foreach (var id in _connectionHandlers.Keys.ToArray())
            {
                _connectionHandlers.TryRemove(id, out _);
                await js.InvokeVoid("BitButil.usb.unsubscribeConnection", id);
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
