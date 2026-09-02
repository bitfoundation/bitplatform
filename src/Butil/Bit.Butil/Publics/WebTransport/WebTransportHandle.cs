using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// An established WebTransport session, returned by <see cref="WebTransport.Connect"/>. Dispose it
/// to close the connection.
/// </summary>
public sealed class WebTransportHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Action _onClosed;
    private bool _closed;

    internal WebTransportHandle(IJSRuntime js, Guid id, Action onClosed)
    {
        _js = js;
        Id = id;
        _onClosed = onClosed;
    }

    /// <summary>The internal session id.</summary>
    public Guid Id { get; }

    /// <summary>Whether the session is still open.</summary>
    public async ValueTask<WebTransportState> GetState()
        => await _js.Invoke<string>("BitButil.webTransport.state", Id) == "open"
            ? WebTransportState.Open
            : WebTransportState.Closed;

    /// <summary>
    /// Sends one datagram - unreliable and unordered, like a UDP packet.
    /// </summary>
    /// <param name="data">
    /// The bytes to send. A datagram larger than the path MTU (about 1200 bytes in practice) cannot
    /// be sent at all, and is dropped rather than fragmented.
    /// </param>
    /// <returns>
    /// False when the datagram could not be queued. True means it was handed to the network, not
    /// that it arrived - nothing about a datagram is acknowledged.
    /// </returns>
    /// <remarks>
    /// Use this for data that is worthless late: position updates, telemetry, media frames. Anything
    /// that has to arrive belongs on a stream (<see cref="OpenStream"/>).
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> SendDatagram(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (_closed) return new ValueTask<bool>(false);
        return _js.Invoke<bool>("BitButil.webTransport.sendDatagram", Id, data);
    }

    /// <summary>
    /// Opens a stream on this session.
    /// </summary>
    /// <param name="bidirectional">
    /// When true the peer can write back on the same stream, and what it writes arrives through the
    /// session's <c>onStreamData</c> callback. A unidirectional stream only sends.
    /// </param>
    /// <returns>The stream, or null when the session is closed.</returns>
    /// <remarks>
    /// Streams are independent of each other: a stall on one does not hold up the rest, which is
    /// the reason to use several rather than multiplex your own framing over one.
    /// </remarks>
    public async ValueTask<WebTransportStream?> OpenStream(bool bidirectional = false)
    {
        if (_closed) return null;

        var streamId = await _js.Invoke<string?>("BitButil.webTransport.openStream", Id, bidirectional);

        return string.IsNullOrEmpty(streamId) ? null : new WebTransportStream(_js, Id, streamId, bidirectional);
    }

    /// <summary>
    /// Closes the session, telling the peer why.
    /// </summary>
    /// <param name="closeCode">An application-defined code the peer receives.</param>
    /// <param name="reason">An application-defined reason string the peer receives (up to 1024 bytes of UTF-8).</param>
    public ValueTask Close(int closeCode = 0, string reason = "") => CloseCore(closeCode, reason);

    /// <summary>Closes the session with no code or reason. Idempotent, and safe during teardown.</summary>
    public async ValueTask DisposeAsync() => await CloseCore(0, string.Empty);

    private async ValueTask CloseCore(int closeCode, string reason)
    {
        if (_closed) return;
        _closed = true;
        _onClosed();
        try { await _js.InvokeVoid("BitButil.webTransport.close", Id, closeCode, reason); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
