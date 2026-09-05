using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Bluetooth_API">Web Bluetooth API</see>
/// (<c>navigator.bluetooth</c>): pick a Bluetooth Low Energy device, connect to its GATT server, and
/// read, write or subscribe to its characteristics.
/// </summary>
/// <remarks>
/// Chromium only, and only over HTTPS. <see cref="RequestDevice"/> opens the browser's device
/// chooser and so must run inside a user gesture; the grant it produces covers exactly the services
/// the request named, which is why <see cref="BluetoothRequestOptions.OptionalServices"/> is
/// usually the property that decides whether a device is usable.
/// </remarks>
[ButilService(typeof(Bluetooth))]
public class Bluetooth(IJSRuntime js) : IAsyncDisposable
{
    internal const string ValueChangedMethodName = nameof(InvokeBluetoothValueChanged);
    internal const string DisconnectedMethodName = nameof(InvokeBluetoothDisconnected);

    // Keyed by subscription id, but carrying the device each one belongs to: releasing a single
    // handle has to drop that device's handlers without touching the others'.
    private readonly ConcurrentDictionary<Guid, (string DeviceId, Action<byte[]> Handler)> _valueHandlers = new();
    private readonly ConcurrentDictionary<Guid, (string DeviceId, Action Handler)> _disconnectHandlers = new();

    // Every handle this service handed out, so a scope/circuit teardown disconnects the radio even
    // when the caller never disposed the device itself.
    private readonly ConcurrentDictionary<string, BluetoothDevice> _devices = new();

    // Per-instance callback reference (see Keyboard): notifications are isolated per circuit /
    // WASM app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Bluetooth>? _dotNetRef;
    private DotNetObjectReference<Bluetooth> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.bluetooth</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.bluetooth.isSupported");

    /// <summary>
    /// True when the machine has a Bluetooth adapter the browser can use. False on a supported
    /// browser whose radio is switched off, which is the case worth telling the user about before
    /// opening a chooser that would be empty.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> GetAvailability() => js.Invoke<bool>("BitButil.bluetooth.getAvailability");

    /// <summary>
    /// Opens the browser's device chooser and returns the device the user picked, or null when
    /// they dismissed it. Must be called from a user gesture.
    /// </summary>
    /// <param name="options">
    /// Which devices the chooser lists, and which services the page may then use. Pass null to
    /// list every nearby device - which grants no services, so it is only useful for discovery.
    /// </param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BluetoothDeviceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BluetoothRequestOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BluetoothFilter))]
    public async ValueTask<BluetoothDevice?> RequestDevice(BluetoothRequestOptions? options = null)
    {
        var info = await js.Invoke<BluetoothDeviceInfo?>("BitButil.bluetooth.requestDevice",
            options ?? new BluetoothRequestOptions { AcceptAllDevices = true });

        return info is null ? null : Track(info);
    }

    /// <summary>
    /// The devices this origin has already been granted, without showing a chooser. Empty on
    /// engines that don't implement <c>getDevices</c> even though they implement the rest.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BluetoothDeviceInfo))]
    public async ValueTask<BluetoothDevice[]> GetDevices()
    {
        var infos = await js.Invoke<BluetoothDeviceInfo[]>("BitButil.bluetooth.getDevices");
        return [.. infos.Select(Track)];
    }

    private BluetoothDevice Track(BluetoothDeviceInfo info)
    {
        var device = new BluetoothDevice(this, js, info);
        _devices[info.Id] = device;
        return device;
    }

    // Called by a handle that is disposing itself, so the service stops holding it. The JS-side
    // release() detaches every listener attached through that handle, so the .NET closures behind
    // them have to go with it - otherwise disposing one device leaks the handlers of that device
    // for as long as the service lives.
    internal void Forget(BluetoothDevice device)
    {
        _devices.TryRemove(device.Id, out _);

        foreach (var pair in _valueHandlers)
        {
            if (pair.Value.DeviceId == device.Id) _valueHandlers.TryRemove(pair.Key, out _);
        }

        foreach (var pair in _disconnectHandlers)
        {
            if (pair.Value.DeviceId == device.Id) _disconnectHandlers.TryRemove(pair.Key, out _);
        }
    }

    /// <summary>
    /// Invoked from JS when a subscribed characteristic's value changes. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ValueChangedMethodName)]
    public void InvokeBluetoothValueChanged(Guid id, byte[] value)
    {
        if (_valueHandlers.TryGetValue(id, out var entry)) entry.Handler.Invoke(value);
    }

    /// <summary>
    /// Invoked from JS on <c>gattserverdisconnected</c>. See <see cref="InvokeBluetoothValueChanged"/>.
    /// </summary>
    [JSInvokable(DisconnectedMethodName)]
    public void InvokeBluetoothDisconnected(Guid id)
    {
        if (_disconnectHandlers.TryGetValue(id, out var entry)) entry.Handler.Invoke();
    }

    [DynamicDependency(nameof(InvokeBluetoothValueChanged), typeof(Bluetooth))]
    internal async ValueTask<ButilSubscription> SubscribeValueChanged(string deviceId, string serviceUuid, string characteristicUuid, Action<byte[]> handler)
    {
        var id = Guid.NewGuid();
        _valueHandlers[id] = (deviceId, handler);

        bool started;
        try
        {
            started = await js.InvokeRegister("BitButil.bluetooth.startNotifications", id, DotNetRef, deviceId, serviceUuid, characteristicUuid);
        }
        catch
        {
            // Nothing is notifying on the JS side, so the entry must not outlive the call.
            _valueHandlers.TryRemove(id, out _);
            throw;
        }

        if (started is false)
        {
            _valueHandlers.TryRemove(id, out _);
            throw new InvalidOperationException($"Notifications could not be started for characteristic '{characteristicUuid}' of service '{serviceUuid}'.");
        }

        return new ButilSubscription(id, async () =>
        {
            _valueHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.bluetooth.stopNotifications", id);
        });
    }

    [DynamicDependency(nameof(InvokeBluetoothDisconnected), typeof(Bluetooth))]
    internal async ValueTask<ButilSubscription> SubscribeDisconnected(string deviceId, Action handler)
    {
        var id = Guid.NewGuid();
        _disconnectHandlers[id] = (deviceId, handler);

        bool subscribed;
        try
        {
            subscribed = await js.InvokeRegister("BitButil.bluetooth.subscribeDisconnect", id, DotNetRef, deviceId);
        }
        catch
        {
            // Nothing is listening on the JS side, so the entry must not outlive the call.
            _disconnectHandlers.TryRemove(id, out _);
            throw;
        }

        if (subscribed is false)
        {
            _disconnectHandlers.TryRemove(id, out _);
            throw new InvalidOperationException("The disconnect listener could not be attached - the device handle is no longer known.");
        }

        return new ButilSubscription(id, async () =>
        {
            _disconnectHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.bluetooth.unsubscribeDisconnect", id);
        });
    }

    /// <summary>
    /// Disconnects every device this service handed out, detaches its listeners and releases the
    /// interop reference.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            foreach (var id in _valueHandlers.Keys.ToArray())
            {
                _valueHandlers.TryRemove(id, out _);
                await js.InvokeVoid("BitButil.bluetooth.stopNotifications", id);
            }

            foreach (var id in _disconnectHandlers.Keys.ToArray())
            {
                _disconnectHandlers.TryRemove(id, out _);
                await js.InvokeVoid("BitButil.bluetooth.unsubscribeDisconnect", id);
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
