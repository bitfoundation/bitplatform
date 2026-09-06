using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A serial port the user picked, and the reads and writes that can be run against it. Obtained
/// from <see cref="Serial.RequestPort"/> or <see cref="Serial.GetPorts"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SerialPort">SerialPort</see>
/// </summary>
/// <remarks>
/// The port must be opened with <see cref="Open"/> before any transfer. Reading is a subscription
/// rather than a call: bytes arrive whenever the device sends them, in whatever chunks the driver
/// hands over, so framing a protocol on top is the caller's job.
/// <br/>
/// Disposing the handle stops the read loop, closes the port and drops the browser-side reference;
/// the user's grant survives. Use <see cref="Forget"/> to revoke the grant itself.
/// </remarks>
public sealed class SerialPort : IAsyncDisposable
{
    private readonly Serial _owner;
    private readonly IJSRuntime _js;
    private bool _disposed;

    internal SerialPort(Serial owner, IJSRuntime js, SerialPortInfo info)
    {
        _owner = owner;
        _js = js;
        Info = info;
    }

    /// <summary>The port as it was when the handle was created.</summary>
    public SerialPortInfo Info { get; }

    /// <summary>The browser-side handle id every operation is routed through.</summary>
    public string Id => Info.Id;

    /// <summary>
    /// Opens the port with the given line settings. Already-open is not an error: re-opening with the
    /// settings already in force does nothing. Re-opening with <em>different</em> settings closes and
    /// re-opens the port, because the API has no way to change them in place - which ends the read
    /// loop, so a <see cref="SubscribeData"/> subscription taken before it stops delivering and has to
    /// be disposed and taken again.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SerialOpenJsOptions))]
    public ValueTask<bool> Open(SerialOptions? options = null)
    {
        options ??= new SerialOptions();

        return _js.Invoke<bool>("BitButil.serial.open", Id, new SerialOpenJsOptions
        {
            BaudRate = options.BaudRate,
            DataBits = options.DataBits,
            StopBits = options.StopBits,
            Parity = options.Parity switch
            {
                SerialParity.Even => "even",
                SerialParity.Odd => "odd",
                _ => "none"
            },
            BufferSize = options.BufferSize,
            FlowControl = options.FlowControl == SerialFlowControl.Hardware ? "hardware" : "none"
        });
    }

    /// <summary>Stops any read loop and closes the port.</summary>
    public ValueTask Close() => _js.InvokeVoid("BitButil.serial.close", Id);

    /// <summary>True while the port is open.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsOpen() => _js.Invoke<bool>("BitButil.serial.isOpen", Id);

    /// <summary>
    /// Re-reads the port's state. <see cref="Info"/> is the snapshot from when the handle was
    /// created and does not update on its own.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SerialPortInfo))]
    public ValueTask<SerialPortInfo?> GetInfo() => _js.Invoke<SerialPortInfo?>("BitButil.serial.getInfo", Id);

    /// <summary>Writes bytes to the port.</summary>
    public ValueTask<bool> Write(byte[] data) => _js.Invoke<bool>("BitButil.serial.write", Id, data);

    /// <summary>Writes UTF-8 text to the port - the usual case for a line-oriented device.</summary>
    public ValueTask<bool> WriteText(string text) => _js.Invoke<bool>("BitButil.serial.writeText", Id, text);

    /// <summary>
    /// Starts reading. <paramref name="onData"/> is called with each chunk exactly as the driver
    /// delivered it, so a message split across two chunks arrives as two calls.
    /// </summary>
    /// <param name="onData">Called for every chunk read from the port.</param>
    /// <param name="onError">Called when the read loop fails - an unplugged adapter, a framing error.</param>
    /// <exception cref="InvalidOperationException">
    /// Reading did not start - the port is closed, or a read loop is already running on it.
    /// <paramref name="onError"/> is called with the same message first.
    /// </exception>
    /// <returns>A subscription - dispose it to stop reading and release the stream lock.</returns>
    public ValueTask<ButilSubscription> SubscribeData(Action<byte[]> onData, Action<string>? onError = null)
        => _owner.SubscribeData(Id, onData, onError);

    /// <summary>The state of the port's input control lines. Null when the port is not open.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SerialSignals))]
    public ValueTask<SerialSignals?> GetSignals() => _js.Invoke<SerialSignals?>("BitButil.serial.getSignals", Id);

    /// <summary>
    /// Drives the port's output control lines. A null argument leaves that line as it is - many
    /// boards are reset by toggling DTR, which is exactly what has to not happen by accident.
    /// </summary>
    /// <param name="dataTerminalReady">DTR.</param>
    /// <param name="requestToSend">RTS.</param>
    /// <param name="signalBreak">The break condition.</param>
    public ValueTask<bool> SetSignals(bool? dataTerminalReady = null, bool? requestToSend = null, bool? signalBreak = null)
        => _js.Invoke<bool>("BitButil.serial.setSignals", Id, dataTerminalReady, requestToSend, signalBreak);

    /// <summary>
    /// Revokes this origin's permission for the port, so it stops appearing in
    /// <see cref="Serial.GetPorts"/> until the user picks it again.
    /// </summary>
    public async ValueTask<bool> Forget()
    {
        var forgotten = await _js.Invoke<bool>("BitButil.serial.forget", Id);

        // JS released its handle, so the id is dead - drop the tracked port so nothing hands it
        // out again. A refused forget leaves the port exactly as it was.
        if (forgotten) _owner.Forget(this);

        return forgotten;
    }

    /// <summary>Stops reading, closes the port and releases the browser-side reference.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _owner.Forget(this);
        try { await _js.InvokeVoid("BitButil.serial.release", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
