using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A HID device the user picked, and the reports that can be exchanged with it. Obtained from
/// <see cref="Hid.RequestDevice"/> or <see cref="Hid.GetDevices"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HIDDevice">HIDDevice</see>
/// </summary>
/// <remarks>
/// The device must be opened with <see cref="Open"/> before any report is sent or received.
/// <br/>
/// Disposing the handle closes the device and drops the browser-side reference; the user's grant
/// survives. Use <see cref="Forget"/> to revoke the grant itself.
/// </remarks>
public sealed class HidDevice : IAsyncDisposable
{
    private readonly Hid _owner;
    private readonly IJSRuntime _js;
    private bool _disposed;

    internal HidDevice(Hid owner, IJSRuntime js, HidDeviceInfo info)
    {
        _owner = owner;
        _js = js;
        Info = info;
    }

    /// <summary>The device as it was when the handle was created, including its collections.</summary>
    public HidDeviceInfo Info { get; }

    /// <summary>The browser-side handle id every operation is routed through.</summary>
    public string Id => Info.Id;

    /// <summary>The product string, when the device reports one.</summary>
    public string? ProductName => Info.ProductName;

    /// <summary>Opens the device. Required before any report; already-open is not an error.</summary>
    public ValueTask<bool> Open() => _js.Invoke<bool>("BitButil.hid.open", Id);

    /// <summary>Closes the device.</summary>
    public ValueTask Close() => _js.InvokeVoid("BitButil.hid.close", Id);

    /// <summary>True while the page has the device open.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsOpened() => _js.Invoke<bool>("BitButil.hid.isOpened", Id);

    /// <summary>
    /// Re-reads the device's state. <see cref="Info"/> is the snapshot from when the handle was
    /// created and does not update on its own.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidDeviceInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidCollectionInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HidReportInfo))]
    public ValueTask<HidDeviceInfo?> GetInfo() => _js.Invoke<HidDeviceInfo?>("BitButil.hid.getInfo", Id);

    /// <summary>
    /// Sends an output report - what drives an LED, a rumble motor or a display.
    /// </summary>
    /// <param name="reportId">The report id, or 0 on a device whose reports are unnumbered.</param>
    /// <param name="data">The payload, without the report id byte.</param>
    public ValueTask<bool> SendReport(byte reportId, byte[] data)
        => _js.Invoke<bool>("BitButil.hid.sendReport", Id, reportId, data);

    /// <summary>Sends a feature report - configuration rather than an event.</summary>
    public ValueTask<bool> SendFeatureReport(byte reportId, byte[] data)
        => _js.Invoke<bool>("BitButil.hid.sendFeatureReport", Id, reportId, data);

    /// <summary>Reads a feature report back from the device. Null when the device refuses it.</summary>
    public ValueTask<byte[]?> ReceiveFeatureReport(byte reportId)
        => _js.Invoke<byte[]?>("BitButil.hid.receiveFeatureReport", Id, reportId);

    /// <summary>
    /// Subscribes to the device's input reports - the events it sends unprompted. The device has to
    /// be open first.
    /// </summary>
    /// <exception cref="InvalidOperationException">The listener was not attached - the handle is no longer known.</exception>
    /// <returns>A subscription - dispose it to detach the listener.</returns>
    public ValueTask<ButilSubscription> SubscribeInputReports(Action<HidInputReport> handler)
        => _owner.SubscribeInputReports(Id, handler);

    /// <summary>
    /// Revokes this origin's permission for the device, so it stops appearing in
    /// <see cref="Hid.GetDevices"/> until the user picks it again.
    /// </summary>
    public async ValueTask<bool> Forget()
    {
        var forgotten = await _js.Invoke<bool>("BitButil.hid.forget", Id);

        // JS released its handle, so the id is dead - drop the tracked device so nothing hands it
        // out again. A refused forget leaves the device exactly as it was.
        if (forgotten) _owner.Forget(this);

        return forgotten;
    }

    /// <summary>Closes the device, detaches its listeners and releases the browser-side reference.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _owner.Forget(this);
        try { await _js.InvokeVoid("BitButil.hid.release", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
