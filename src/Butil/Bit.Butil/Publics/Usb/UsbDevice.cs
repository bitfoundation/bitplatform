using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A USB device the user picked, and the transfers that can be run against it. Obtained from
/// <see cref="Usb.RequestDevice"/> or <see cref="Usb.GetDevices"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/USBDevice">USBDevice</see>
/// </summary>
/// <remarks>
/// The order the API expects is fixed: <see cref="Open"/>, then <see cref="SelectConfiguration"/>,
/// then <see cref="ClaimInterface"/>, and only then any transfer. Skipping a step fails with an
/// <c>InvalidStateError</c> rather than a hint about which step was missed.
/// <br/>
/// Disposing the handle closes the device and drops the browser-side reference; the user's grant
/// survives, so a later <see cref="Usb.GetDevices"/> still finds it. Use <see cref="Forget"/> to
/// revoke the grant itself.
/// </remarks>
public sealed class UsbDevice : IAsyncDisposable
{
    private readonly Usb _owner;
    private readonly IJSRuntime _js;
    private bool _disposed;

    internal UsbDevice(Usb owner, IJSRuntime js, UsbDeviceInfo info)
    {
        _owner = owner;
        _js = js;
        Info = info;
    }

    /// <summary>The device as it was when the handle was created, including its descriptor tree.</summary>
    public UsbDeviceInfo Info { get; }

    /// <summary>The browser-side handle id every operation is routed through.</summary>
    public string Id => Info.Id;

    /// <summary>Opens the device. Required before anything else; already-open is not an error.</summary>
    public ValueTask<bool> Open() => _js.Invoke<bool>("BitButil.usb.open", Id);

    /// <summary>Closes the device, releasing it back to the operating system.</summary>
    public ValueTask Close() => _js.InvokeVoid("BitButil.usb.close", Id);

    /// <summary>True while the page has the device open.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsOpened() => _js.Invoke<bool>("BitButil.usb.isOpened", Id);

    /// <summary>
    /// Re-reads the device's state - which configuration is selected, which interfaces are
    /// claimed. <see cref="Info"/> is the snapshot from when the handle was created and does not
    /// update on its own.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbDeviceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbConfigurationInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbInterfaceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbAlternateInterfaceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbEndpointInfo))]
    public ValueTask<UsbDeviceInfo?> GetInfo() => _js.Invoke<UsbDeviceInfo?>("BitButil.usb.getInfo", Id);

    /// <summary>Selects a configuration by its <see cref="UsbConfigurationInfo.ConfigurationValue"/>.</summary>
    public ValueTask<bool> SelectConfiguration(byte configurationValue)
        => _js.Invoke<bool>("BitButil.usb.selectConfiguration", Id, configurationValue);

    /// <summary>
    /// Claims an interface for exclusive use by this page. Fails when the operating system's own
    /// driver already owns it - which is what happens with keyboards, mice and mass storage.
    /// </summary>
    public ValueTask<bool> ClaimInterface(byte interfaceNumber)
        => _js.Invoke<bool>("BitButil.usb.claimInterface", Id, interfaceNumber);

    /// <summary>Gives an interface back. False when it was not claimed.</summary>
    public ValueTask<bool> ReleaseInterface(byte interfaceNumber)
        => _js.Invoke<bool>("BitButil.usb.releaseInterface", Id, interfaceNumber);

    /// <summary>Switches a claimed interface to one of its alternate settings.</summary>
    public ValueTask<bool> SelectAlternateInterface(byte interfaceNumber, byte alternateSetting)
        => _js.Invoke<bool>("BitButil.usb.selectAlternateInterface", Id, interfaceNumber, alternateSetting);

    /// <summary>Sends a control request and reads up to <paramref name="length"/> bytes back.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbControlTransferParameters))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbTransferResult))]
    public ValueTask<UsbTransferResult?> ControlTransferIn(UsbControlTransferParameters parameters, ushort length)
        => _js.Invoke<UsbTransferResult?>("BitButil.usb.controlTransferIn", Id, parameters, length);

    /// <summary>Sends a control request, optionally with a data stage.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbControlTransferParameters))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbTransferResult))]
    public ValueTask<UsbTransferResult?> ControlTransferOut(UsbControlTransferParameters parameters, byte[]? data = null)
        => _js.Invoke<UsbTransferResult?>("BitButil.usb.controlTransferOut", Id, parameters, data);

    /// <summary>
    /// Reads from a bulk or interrupt IN endpoint. The call waits for the device to have something
    /// to say, so it can stay pending indefinitely - treat it as a read loop, not a poll.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbTransferResult))]
    public ValueTask<UsbTransferResult?> TransferIn(byte endpointNumber, uint length)
        => _js.Invoke<UsbTransferResult?>("BitButil.usb.transferIn", Id, endpointNumber, length);

    /// <summary>Writes to a bulk or interrupt OUT endpoint.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UsbTransferResult))]
    public ValueTask<UsbTransferResult?> TransferOut(byte endpointNumber, byte[] data)
        => _js.Invoke<UsbTransferResult?>("BitButil.usb.transferOut", Id, endpointNumber, data);

    /// <summary>
    /// Clears a halted endpoint, which is the only way out of a <c>"stall"</c> status.
    /// </summary>
    /// <param name="direction"><c>"in"</c> or <c>"out"</c>.</param>
    /// <param name="endpointNumber">The endpoint to clear.</param>
    public ValueTask<bool> ClearHalt(string direction, byte endpointNumber)
        => _js.Invoke<bool>("BitButil.usb.clearHalt", Id, direction, endpointNumber);

    /// <summary>Resets the device, abandoning every pending transfer.</summary>
    public ValueTask<bool> Reset() => _js.Invoke<bool>("BitButil.usb.reset", Id);

    /// <summary>
    /// Revokes this origin's permission for the device, so it stops appearing in
    /// <see cref="Usb.GetDevices"/> until the user picks it again.
    /// </summary>
    public async ValueTask<bool> Forget()
    {
        var forgotten = await _js.Invoke<bool>("BitButil.usb.forget", Id);

        // JS released its handle, so the id is dead - drop the tracked device so nothing hands it
        // out again. A refused forget leaves the device exactly as it was.
        if (forgotten) _owner.Forget(this);

        return forgotten;
    }

    /// <summary>Closes the device and releases the browser-side reference.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _owner.Forget(this);
        try { await _js.InvokeVoid("BitButil.usb.release", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
