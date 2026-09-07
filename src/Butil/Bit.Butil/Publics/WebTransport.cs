using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebTransport_API">WebTransport API</see>:
/// a bidirectional connection to an HTTP/3 server carrying many independent streams and, separately,
/// unreliable datagrams.
/// </summary>
/// <remarks>
/// What it has over a WebSocket is head-of-line blocking, or the lack of it. A WebSocket is one
/// ordered stream over TCP, so a lost packet stalls everything behind it; WebTransport runs over
/// QUIC, where each stream is independent and a datagram is not retransmitted at all. That matters
/// for games, live media and telemetry, and matters not at all for a chat message.
/// <br/>
/// Requires an HTTP/3 server: this cannot talk to an ordinary HTTPS endpoint, and there is no
/// fallback to one. For a development server whose certificate no public CA signed, pass its hash
/// through <c>certificateHashes</c>.
/// <br/>
/// Incoming data is delivered through the callbacks passed to <see cref="Connect"/> rather than
/// read from the handle: the browser's reader is a pull loop, so it runs on the JS side and
/// dispatches what it reads.
/// </remarks>
[ButilService(typeof(WebTransport))]
public class WebTransport(IJSRuntime js) : IAsyncDisposable
{
    internal const string DatagramMethodName = nameof(InvokeWebTransportDatagram);
    internal const string StreamDataMethodName = nameof(InvokeWebTransportStreamData);
    internal const string StreamEndMethodName = nameof(InvokeWebTransportStreamEnd);
    internal const string StreamOpenedMethodName = nameof(InvokeWebTransportStreamOpened);
    internal const string ClosedMethodName = nameof(InvokeWebTransportClosed);

    private readonly ConcurrentDictionary<Guid, SessionHandlers> _handlers = new();

    // Per-instance callback reference (see Keyboard): sessions are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<WebTransport>? _dotNetRef;
    private DotNetObjectReference<WebTransport> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    private sealed record SessionHandlers(
        Action<byte[]>? OnDatagram,
        Action<WebTransportStreamData>? OnStreamData,
        Action<WebTransportStream>? OnStreamOpened,
        Action<WebTransportCloseInfo>? OnClosed);

    /// <summary>True when the runtime exposes <c>WebTransport</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webTransport.isSupported");

    /// <summary>
    /// Invoked from JS for each datagram. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(DatagramMethodName)]
    public void InvokeWebTransportDatagram(Guid id, byte[] data)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnDatagram?.Invoke(data);
    }

    /// <summary>Invoked from JS for each chunk read off a stream. See <see cref="InvokeWebTransportDatagram"/>.</summary>
    [JSInvokable(StreamDataMethodName)]
    public void InvokeWebTransportStreamData(Guid id, string streamId, byte[] data)
    {
        if (_handlers.TryGetValue(id, out var handlers))
            handlers.OnStreamData?.Invoke(new WebTransportStreamData(streamId, data, false));
    }

    /// <summary>Invoked from JS when a stream is finished. See <see cref="InvokeWebTransportDatagram"/>.</summary>
    [JSInvokable(StreamEndMethodName)]
    public void InvokeWebTransportStreamEnd(Guid id, string streamId)
    {
        if (_handlers.TryGetValue(id, out var handlers))
            handlers.OnStreamData?.Invoke(new WebTransportStreamData(streamId, [], true));
    }

    /// <summary>Invoked from JS when the server opens a stream. See <see cref="InvokeWebTransportDatagram"/>.</summary>
    [JSInvokable(StreamOpenedMethodName)]
    public void InvokeWebTransportStreamOpened(Guid id, string streamId, bool bidirectional)
    {
        if (_handlers.TryGetValue(id, out var handlers))
            handlers.OnStreamOpened?.Invoke(new WebTransportStream(js, id, streamId, bidirectional));
    }

    /// <summary>Invoked from JS when the session ends. See <see cref="InvokeWebTransportDatagram"/>.</summary>
    /// <remarks>
    /// This is where a session's handlers are dropped, rather than when <see cref="WebTransportHandle.Close"/>
    /// was called: the closure is dispatched after that call, so removing them any earlier would
    /// swallow the notification for every session this side closed.
    /// </remarks>
    [JSInvokable(ClosedMethodName)]
    public void InvokeWebTransportClosed(Guid id, int closeCode, string reason, string error)
    {
        if (_handlers.TryRemove(id, out var handlers))
            handlers.OnClosed?.Invoke(new WebTransportCloseInfo(closeCode, reason, error));
    }

    /// <summary>
    /// Connects to an HTTP/3 endpoint and waits until the session is usable.
    /// </summary>
    /// <param name="url">An <c>https:</c> URL served by a WebTransport-capable HTTP/3 server.</param>
    /// <param name="onDatagram">Called for each datagram received.</param>
    /// <param name="onStreamData">
    /// Called for each chunk read off any stream of this session, and once more with
    /// <see cref="WebTransportStreamData.Ended"/> when a stream finishes.
    /// </param>
    /// <param name="onStreamOpened">
    /// Called when the <em>server</em> opens a stream. The stream passed in is how to answer on it
    /// when it is bidirectional; streams this side opens come back from
    /// <see cref="WebTransportHandle.OpenStream"/> instead.
    /// </param>
    /// <param name="onClosed">
    /// Called once when the session ends, whichever side ended it and whether it closed or failed.
    /// Not called for a connection that never established - that is this method's own result.
    /// </param>
    /// <param name="allowPooling">Allow reusing an existing QUIC connection to the same host instead of opening a new one.</param>
    /// <param name="congestionControl">What to tune the connection for.</param>
    /// <param name="certificateHashes">
    /// Accept exactly these server certificates rather than the ones a public CA signed - how to
    /// reach a development server. See <see cref="WebTransportCertificateHash"/> for its limits.
    /// </param>
    /// <returns>
    /// The session, or the reason there isn't one. See <see cref="WebTransportConnectResult"/>.
    /// </returns>
    /// <remarks>
    /// Dispose the returned session when you're done; an undisposed one holds the connection open
    /// for as long as the page lives.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebTransportCertificateHash))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebTransportConnectInfo))]
    [DynamicDependency(nameof(InvokeWebTransportDatagram), typeof(WebTransport))]
    [DynamicDependency(nameof(InvokeWebTransportStreamData), typeof(WebTransport))]
    [DynamicDependency(nameof(InvokeWebTransportStreamEnd), typeof(WebTransport))]
    [DynamicDependency(nameof(InvokeWebTransportStreamOpened), typeof(WebTransport))]
    [DynamicDependency(nameof(InvokeWebTransportClosed), typeof(WebTransport))]
    public async ValueTask<WebTransportConnectResult> Connect(string url,
                                                              Action<byte[]>? onDatagram = null,
                                                              Action<WebTransportStreamData>? onStreamData = null,
                                                              Action<WebTransportStream>? onStreamOpened = null,
                                                              Action<WebTransportCloseInfo>? onClosed = null,
                                                              bool allowPooling = false,
                                                              WebTransportCongestionControl congestionControl = WebTransportCongestionControl.Default,
                                                              WebTransportCertificateHash[]? certificateHashes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var id = Guid.NewGuid();
        _handlers[id] = new SessionHandlers(onDatagram, onStreamData, onStreamOpened, onClosed);

        var info = await js.Invoke<WebTransportConnectInfo?>("BitButil.webTransport.connect",
            DotNetRef, id, url, allowPooling, CongestionControl(congestionControl), certificateHashes ?? []);

        if (info?.Connected is not true)
        {
            _handlers.TryRemove(id, out _);
            return new WebTransportConnectResult(null, info?.Error ?? "the connection could not be established");
        }

        return new WebTransportConnectResult(
            new WebTransportHandle(js, id, () => _handlers.TryRemove(id, out _)),
            string.Empty);
    }

    /// <summary>
    /// On scope/circuit teardown, closes any session whose <see cref="WebTransportHandle"/> was
    /// never disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.webTransport.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private static string? CongestionControl(WebTransportCongestionControl congestionControl) => congestionControl switch
    {
        WebTransportCongestionControl.Throughput => "throughput",
        WebTransportCongestionControl.LowLatency => "low-latency",
        _ => null,
    };
}
