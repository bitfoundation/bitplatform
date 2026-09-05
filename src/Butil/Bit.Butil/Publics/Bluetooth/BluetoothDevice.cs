using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A device the user picked in the Bluetooth chooser, and the GATT operations that can be run
/// against it. Obtained from <see cref="Bluetooth.RequestDevice"/> or <see cref="Bluetooth.GetDevices"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BluetoothDevice">BluetoothDevice</see>
/// </summary>
/// <remarks>
/// Disposing the handle disconnects the GATT server and drops the browser-side reference; the
/// user's grant survives, so a later <see cref="Bluetooth.GetDevices"/> still finds the device.
/// Use <see cref="Forget"/> to revoke the grant itself.
/// <br/>
/// Every operation re-connects if the link has dropped, because a GATT server disconnects on its
/// own far more often than a caller would expect.
/// </remarks>
public sealed class BluetoothDevice : IAsyncDisposable
{
    private readonly Bluetooth _owner;
    private readonly IJSRuntime _js;
    private bool _disposed;

    internal BluetoothDevice(Bluetooth owner, IJSRuntime js, BluetoothDeviceInfo info)
    {
        _owner = owner;
        _js = js;
        Info = info;
    }

    /// <summary>The device as it was when the handle was created - id, name and connection state.</summary>
    public BluetoothDeviceInfo Info { get; }

    /// <summary>The browser-side handle id every operation is routed through.</summary>
    public string Id => Info.Id;

    /// <summary>The advertised device name, or null when the device advertises none.</summary>
    public string? Name => Info.Name;

    /// <summary>Opens the GATT connection. Returns false when the device is out of range or refuses.</summary>
    public ValueTask<bool> Connect() => _js.Invoke<bool>("BitButil.bluetooth.connect", Id);

    /// <summary>Closes the GATT connection without giving up the permission grant.</summary>
    public ValueTask Disconnect() => _js.InvokeVoid("BitButil.bluetooth.disconnect", Id);

    /// <summary>True while the GATT server is connected right now.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsConnected() => _js.Invoke<bool>("BitButil.bluetooth.isConnected", Id);

    /// <summary>
    /// The device's primary services. Only services covered by the grant are listed - see
    /// <see cref="BluetoothRequestOptions.OptionalServices"/>.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BluetoothServiceInfo))]
    public ValueTask<BluetoothServiceInfo[]> GetServices()
        => _js.Invoke<BluetoothServiceInfo[]>("BitButil.bluetooth.getPrimaryServices", Id);

    /// <summary>The characteristics of one service, each with the operations it supports.</summary>
    /// <param name="serviceUuid">A full UUID, a 16-bit alias (<c>"0x180d"</c>) or a registered name (<c>"heart_rate"</c>).</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BluetoothCharacteristicInfo))]
    public ValueTask<BluetoothCharacteristicInfo[]> GetCharacteristics(string serviceUuid)
        => _js.Invoke<BluetoothCharacteristicInfo[]>("BitButil.bluetooth.getCharacteristics", Id, serviceUuid);

    /// <summary>Reads a characteristic's current value. Null when the characteristic can't be resolved.</summary>
    public ValueTask<byte[]?> Read(string serviceUuid, string characteristicUuid)
        => _js.Invoke<byte[]?>("BitButil.bluetooth.readValue", Id, serviceUuid, characteristicUuid);

    /// <summary>Writes a characteristic's value.</summary>
    /// <param name="serviceUuid">The service holding the characteristic.</param>
    /// <param name="characteristicUuid">The characteristic to write.</param>
    /// <param name="data">The bytes to write.</param>
    /// <param name="withResponse">
    /// True waits for the device to acknowledge the write; false is fire-and-forget and much
    /// faster, but only where <see cref="BluetoothCharacteristicInfo.WriteWithoutResponse"/> is set.
    /// </param>
    public ValueTask<bool> Write(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true)
        => _js.Invoke<bool>("BitButil.bluetooth.writeValue", Id, serviceUuid, characteristicUuid, data, withResponse);

    /// <summary>
    /// Subscribes to a characteristic's notifications - the device pushing new values as they
    /// change, instead of the page polling. Needs <see cref="BluetoothCharacteristicInfo.Notify"/>
    /// or <see cref="BluetoothCharacteristicInfo.Indicate"/>.
    /// </summary>
    /// <returns>A subscription - dispose it to stop the notifications.</returns>
    /// <exception cref="InvalidOperationException">
    /// Notifications did not start - the characteristic could not be resolved, or it does not
    /// notify.
    /// </exception>
    public ValueTask<ButilSubscription> SubscribeValueChanged(string serviceUuid, string characteristicUuid, Action<byte[]> handler)
        => _owner.SubscribeValueChanged(Id, serviceUuid, characteristicUuid, handler);

    /// <summary>Runs <paramref name="handler"/> when the device drops the GATT connection.</summary>
    /// <returns>A subscription - dispose it to detach the listener.</returns>
    /// <exception cref="InvalidOperationException">The listener was not attached - the handle is no longer known.</exception>
    public ValueTask<ButilSubscription> SubscribeDisconnected(Action handler)
        => _owner.SubscribeDisconnected(Id, handler);

    /// <summary>
    /// Revokes this origin's permission for the device, so it stops appearing in
    /// <see cref="Bluetooth.GetDevices"/> until the user picks it again. Not implemented by every
    /// Chromium version - false means it was not available.
    /// </summary>
    public async ValueTask<bool> Forget()
    {
        var forgotten = await _js.Invoke<bool>("BitButil.bluetooth.forget", Id);

        // JS released its handle, so the id is dead - drop the tracked device, and with it the
        // handler closures the service holds for it. A refused forget leaves the device as it was.
        if (forgotten) _owner.Forget(this);

        return forgotten;
    }

    /// <summary>Disconnects, detaches this device's listeners and releases the browser-side reference.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _owner.Forget(this);
        try { await _js.InvokeVoid("BitButil.bluetooth.release", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
