using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// An open connection, returned by <see cref="WebSocket.Open"/>. Dispose it to close the socket.
/// </summary>
public sealed class WebSocketHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Action _onClosed;
    private bool _closed;

    internal WebSocketHandle(IJSRuntime js, Guid id, Action onClosed)
    {
        _js = js;
        Id = id;
        _onClosed = onClosed;
    }

    /// <summary>The internal socket id.</summary>
    public Guid Id { get; }

    /// <summary>The connection's current state.</summary>
    /// <remarks><see cref="WebSocketState.Closed"/> is also what a disposed handle reports.</remarks>
    public async ValueTask<WebSocketState> GetState()
        => (WebSocketState)await _js.Invoke<int>("BitButil.webSocket.readyState", Id);

    /// <summary>
    /// Bytes handed to <see cref="SendText"/>/<see cref="SendBytes"/> that are still queued rather
    /// than on the wire.
    /// </summary>
    /// <remarks>
    /// The only back-pressure signal a browser socket offers: sending never blocks and never fails
    /// for being too fast, so a producer that outruns the connection simply grows this number until
    /// the tab runs out of memory. Check it before sending in a loop.
    /// </remarks>
    public ValueTask<long> GetBufferedAmount() => _js.Invoke<long>("BitButil.webSocket.bufferedAmount", Id);

    /// <summary>
    /// The sub-protocol the server chose, or an empty string when none was negotiated. Also handed
    /// to the <c>onOpen</c> callback.
    /// </summary>
    public ValueTask<string> GetProtocol() => _js.Invoke<string>("BitButil.webSocket.protocol", Id);

    /// <summary>The extensions the server agreed to (for example <c>permessage-deflate</c>), or an empty string.</summary>
    public ValueTask<string> GetExtensions() => _js.Invoke<string>("BitButil.webSocket.extensions", Id);

    /// <summary>The absolute URL the socket resolved to.</summary>
    public ValueTask<string> GetUrl() => _js.Invoke<string>("BitButil.webSocket.url", Id);

    /// <summary>
    /// Sends a text frame.
    /// </summary>
    /// <returns>
    /// False when the socket is not open - including the ordinary race of sending before the
    /// connection has been established, which throws in JavaScript rather than queueing.
    /// </returns>
    public ValueTask<bool> SendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return _js.Invoke<bool>("BitButil.webSocket.sendText", Id, text);
    }

    /// <summary>
    /// Sends a binary frame.
    /// </summary>
    /// <returns>False when the socket is not open.</returns>
    /// <remarks>
    /// Incoming binary frames arrive as <see cref="WebSocketMessage.Data"/> without a round trip
    /// through a Blob, because the socket is put in <c>arraybuffer</c> mode when it is opened.
    /// </remarks>
    public ValueTask<bool> SendBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return _js.Invoke<bool>("BitButil.webSocket.sendBytes", Id, data);
    }

    /// <summary>
    /// Closes the connection, sending a close frame.
    /// </summary>
    /// <param name="code">
    /// A close code. Only <c>1000</c> ("normal") and <c>3000</c>-<c>4999</c> (application-defined)
    /// may be sent from script; anything else is refused, and the socket is closed without a code
    /// instead. Left null, no code is sent, which the other end sees as <c>1005</c>.
    /// </param>
    /// <param name="reason">A short explanation for the peer. At most 123 UTF-8 bytes.</param>
    /// <remarks>
    /// The <c>onClose</c> callback still runs after this - closing locally and being closed remotely
    /// are the same event.
    /// </remarks>
    public ValueTask Close(int? code = null, string? reason = null)
        => _js.InvokeVoid("BitButil.webSocket.close", Id, code, reason);

    /// <summary>
    /// Closes the connection. Idempotent, and safe during teardown.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_closed) return;
        _closed = true;

        // The close event is asynchronous, and it is what runs the onClose callback and unregisters
        // the handlers. Releasing them here would make disposal the one close nobody hears about -
        // so the handlers are only dropped by hand when the close cannot be asked for at all.
        try { await _js.InvokeVoid("BitButil.webSocket.close", Id, null, null); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) // teardown: circuit gone, cancelled, or already disposed
        {
            _onClosed();
        }
    }
}
