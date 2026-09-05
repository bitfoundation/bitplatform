using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// One stream inside a WebTransport session: an ordered, reliable byte stream, opened by
/// <see cref="WebTransportHandle.OpenStream"/> or handed to the session's <c>onStreamOpened</c>
/// callback when the server opens one.
/// </summary>
/// <remarks>
/// Data arriving on a stream is delivered to the session's <c>onStreamData</c> callback rather than
/// read from here - the browser's reader is a pull loop that has to keep running, so it runs on the
/// JS side and dispatches what it reads.
/// <br/>
/// Disposing closes the writable half, which is what tells the peer the message is complete.
/// </remarks>
public sealed class WebTransportStream : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _sessionId;
    private bool _closed;

    internal WebTransportStream(IJSRuntime js, Guid sessionId, string id, bool isBidirectional)
    {
        _js = js;
        _sessionId = sessionId;
        Id = id;
        IsBidirectional = isBidirectional;
    }

    /// <summary>The stream's id within its session, as it appears in <see cref="WebTransportStreamData.StreamId"/>.</summary>
    public string Id { get; }

    /// <summary>
    /// True when the peer can write back on this stream. A unidirectional stream this side opened
    /// only sends; one the peer opened only receives, and <see cref="Write"/> on it does nothing.
    /// </summary>
    public bool IsBidirectional { get; }

    /// <summary>
    /// Writes bytes to the stream. Ordered and reliable - unlike
    /// <see cref="WebTransportHandle.SendDatagram"/>, nothing here is dropped or reordered.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <returns>False when the stream is closed, has no writable half, or the session is gone.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Write(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (_closed) return new ValueTask<bool>(false);
        return _js.Invoke<bool>("BitButil.webTransport.writeStream", _sessionId, Id, data);
    }

    /// <summary>Closes the stream's writable half. Idempotent, and safe during teardown.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_closed) return;
        _closed = true;
        try { await _js.InvokeVoid("BitButil.webTransport.closeStream", _sessionId, Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
