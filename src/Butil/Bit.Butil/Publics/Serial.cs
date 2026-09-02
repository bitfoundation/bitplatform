using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Serial_API">Web Serial API</see>
/// (<c>navigator.serial</c>): pick a serial port, open it with the line settings the device
/// expects, and read and write bytes.
/// </summary>
/// <remarks>
/// Chromium only, and only over HTTPS. <see cref="RequestPort"/> opens the browser's port chooser
/// and so must run inside a user gesture.
/// </remarks>
[ButilService(typeof(Serial))]
public class Serial(IJSRuntime js) : IAsyncDisposable
{
    internal const string DataMethodName = nameof(InvokeSerialData);
    internal const string ErrorMethodName = nameof(InvokeSerialError);
    internal const string ConnectedMethodName = nameof(InvokeSerialConnected);
    internal const string DisconnectedMethodName = nameof(InvokeSerialDisconnected);

    private readonly ConcurrentDictionary<Guid, (Action<byte[]> OnData, Action<string>? OnError)> _dataHandlers = new();
    private readonly ConcurrentDictionary<Guid, (Action<SerialPort>? OnConnected, Action<SerialPort>? OnDisconnected)> _connectionHandlers = new();

    // Every handle this service handed out, so a scope/circuit teardown closes the port even when
    // the caller never disposed it.
    private readonly ConcurrentDictionary<string, SerialPort> _ports = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Serial>? _dotNetRef;
    private DotNetObjectReference<Serial> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.serial</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.serial.isSupported");

    /// <summary>
    /// Opens the browser's port chooser and returns the port the user picked, or null when they
    /// dismissed it. Must be called from a user gesture.
    /// </summary>
    /// <param name="filters">
    /// Which ports the chooser lists. Pass none to list every port - the usual choice, since a
    /// machine rarely has enough of them for filtering to help.
    /// </param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SerialPortInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SerialPortFilter))]
    public async ValueTask<SerialPort?> RequestPort(params SerialPortFilter[] filters)
    {
        var info = await js.Invoke<SerialPortInfo?>("BitButil.serial.requestPort", (object)filters);
        return info is null ? null : Track(info);
    }

    /// <summary>The ports this origin has already been granted, without showing a chooser.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SerialPortInfo))]
    public async ValueTask<SerialPort[]> GetPorts()
    {
        var infos = await js.Invoke<SerialPortInfo[]>("BitButil.serial.getPorts");
        return [.. infos.Select(Track)];
    }

    private SerialPort Track(SerialPortInfo info)
    {
        var port = new SerialPort(this, js, info);
        _ports[info.Id] = port;
        return port;
    }

    // Called by a handle that is disposing itself, so the service stops holding it.
    internal void Forget(SerialPort port) => _ports.TryRemove(port.Id, out _);

    /// <summary>
    /// Invoked from JS for each chunk read from a port. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(DataMethodName)]
    public void InvokeSerialData(Guid id, byte[] data)
    {
        if (_dataHandlers.TryGetValue(id, out var handlers)) handlers.OnData.Invoke(data);
    }

    /// <summary>Invoked from JS when a read loop fails. See <see cref="InvokeSerialData"/>.</summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeSerialError(Guid id, string message)
    {
        if (_dataHandlers.TryGetValue(id, out var handlers)) handlers.OnError?.Invoke(message);
    }

    /// <summary>Invoked from JS on <c>navigator.serial</c>'s <c>connect</c>. See <see cref="InvokeSerialData"/>.</summary>
    [JSInvokable(ConnectedMethodName)]
    public void InvokeSerialConnected(Guid id, SerialPortInfo info)
    {
        if (_connectionHandlers.TryGetValue(id, out var handlers)) handlers.OnConnected?.Invoke(Track(info));
    }

    /// <summary>Invoked from JS on <c>disconnect</c>. See <see cref="InvokeSerialData"/>.</summary>
    [JSInvokable(DisconnectedMethodName)]
    public void InvokeSerialDisconnected(Guid id, SerialPortInfo info)
    {
        if (_connectionHandlers.TryGetValue(id, out var handlers)) handlers.OnDisconnected?.Invoke(Track(info));
    }

    [DynamicDependency(nameof(InvokeSerialData), typeof(Serial))]
    [DynamicDependency(nameof(InvokeSerialError), typeof(Serial))]
    internal async ValueTask<ButilSubscription> SubscribeData(string portId, Action<byte[]> onData, Action<string>? onError)
    {
        var id = Guid.NewGuid();
        _dataHandlers[id] = (onData, onError);
        await js.InvokeVoid("BitButil.serial.startReading", id, DotNetRef, portId);

        return new ButilSubscription(id, async () =>
        {
            _dataHandlers.TryRemove(id, out _);
            // Reading is keyed by the port, not the subscription: the stream lock is exclusive, so
            // there is at most one read loop per port to stop.
            await js.InvokeVoid("BitButil.serial.stopReading", portId);
        });
    }

    /// <summary>
    /// Watches ports appearing and disappearing - a USB serial adapter being plugged in or
    /// unplugged. Only ports this origin already has permission for raise these.
    /// </summary>
    /// <returns>A subscription - dispose it to detach the listeners.</returns>
    [DynamicDependency(nameof(InvokeSerialConnected), typeof(Serial))]
    [DynamicDependency(nameof(InvokeSerialDisconnected), typeof(Serial))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SerialPortInfo))]
    public async ValueTask<ButilSubscription> SubscribeConnection(Action<SerialPort>? onConnected = null,
                                                                  Action<SerialPort>? onDisconnected = null)
    {
        var id = Guid.NewGuid();
        _connectionHandlers[id] = (onConnected, onDisconnected);
        await js.InvokeVoid("BitButil.serial.subscribeConnection", id, DotNetRef);

        return new ButilSubscription(id, async () =>
        {
            _connectionHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.serial.unsubscribeConnection", id);
        });
    }

    /// <summary>
    /// Closes every port this service handed out, detaches its listeners and releases the interop
    /// reference.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _dataHandlers.Clear();

            foreach (var id in _connectionHandlers.Keys.ToArray())
            {
                _connectionHandlers.TryRemove(id, out _);
                await js.InvokeVoid("BitButil.serial.unsubscribeConnection", id);
            }

            // Releasing a port stops its read loop, which is what actually detaches the data
            // handlers cleared above.
            foreach (var port in _ports.Values.ToArray())
            {
                await port.DisposeAsync();
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
