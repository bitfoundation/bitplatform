using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A data channel to the peer - a socket to another browser rather than to a server.
/// </summary>
/// <remarks>
/// Nothing can be sent until the channel opens, which happens once the connection is established;
/// subscribe with <see cref="Listen"/> before the handshake - or, for a channel the peer created,
/// from inside the <c>onRemoteChannel</c> callback - so the open is not missed.
/// <br/>
/// The reason to use one rather than a <see cref="WebSocket"/> is what it can give up: an unordered,
/// non-retransmitting channel delivers what is current rather than what is complete, which no
/// TCP-based transport can offer at all.
/// </remarks>
public sealed class RtcDataChannelHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly WebRtc _owner;
    private bool _closed;

    internal RtcDataChannelHandle(IJSRuntime js, WebRtc owner, Guid id, string label)
    {
        _js = js;
        _owner = owner;
        Id = id;
        Label = label;
    }

    /// <summary>The internal channel id.</summary>
    public Guid Id { get; }

    /// <summary>The channel's label, which both sides see. It is how one channel is told from another.</summary>
    public string Label { get; }

    /// <summary>
    /// Attaches the callbacks for this channel.
    /// </summary>
    /// <param name="onMessage">Called for every message. <see cref="ButilMessage.IsBinary"/> says which payload carries it.</param>
    /// <param name="onOpen">Called when the channel is ready to send. Sending before this fails.</param>
    /// <param name="onClose">Called when it closes, from either end.</param>
    /// <remarks>
    /// For a channel you created, its events are held from the moment it was made until this call,
    /// so the open is not lost to the round trip that handed you the handle - which is the ordinary
    /// case when the connection is already established. They are held for at most ten seconds: a
    /// channel nobody ever listens to must not queue its messages for ever.
    /// <br/>
    /// For a channel the peer created - one handed to <c>onRemoteChannel</c> - the channel is
    /// already open by the time you see it, so its events are held until that callback returns and
    /// are delivered afterwards. Calling this inside <c>onRemoteChannel</c> therefore misses nothing,
    /// not even the open, as long as it is called before the callback returns; calling it later,
    /// after an <c>await</c> or from another turn, still misses whatever arrived in between.
    /// </remarks>
    public void Listen(Action<ButilMessage>? onMessage = null, Action? onOpen = null, Action? onClose = null)
    {
        _owner.SetChannelHandlers(Id, onOpen, onClose, onMessage);

        // Registering the handlers is what releases the events held since the channel was made.
        // Started rather than awaited, because this has to stay synchronous: called from inside
        // onRemoteChannel, an await here would return to the caller and let JS flush first, which
        // is the very loss the deferral exists to prevent. The flush itself is a no-op when there
        // is nothing held, so a channel that was already flushed pays only the round trip.
        _ = Flush();
    }

    private async ValueTask Flush()
    {
        try { await _js.InvokeVoid("BitButil.webRtc.flushChannel", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // the circuit or the channel is already gone
    }

    /// <summary>
    /// Sends text.
    /// </summary>
    /// <returns>False when the channel is not open - including the ordinary race of sending before the connection is established.</returns>
    public ValueTask<bool> SendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return _js.Invoke<bool>("BitButil.webRtc.sendText", Id, text);
    }

    /// <summary>
    /// Sends bytes.
    /// </summary>
    /// <returns>False when the channel is not open.</returns>
    /// <remarks>
    /// Incoming binary arrives as <see cref="ButilMessage.Data"/> without a round trip through a
    /// Blob, because the channel is put in <c>arraybuffer</c> mode when it is created.
    /// </remarks>
    public ValueTask<bool> SendBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return _js.Invoke<bool>("BitButil.webRtc.sendBytes", Id, data);
    }

    /// <summary>
    /// <c>"connecting"</c>, <c>"open"</c>, <c>"closing"</c> or <c>"closed"</c>. Also
    /// <c>"closed"</c> for a channel that has been released.
    /// </summary>
    public ValueTask<string> GetState() => _js.Invoke<string>("BitButil.webRtc.channelState", Id);

    /// <summary>
    /// Bytes queued but not yet sent - the same back-pressure signal a
    /// <see cref="WebSocketHandle.GetBufferedAmount"/> gives, and the same warning: sending never
    /// blocks, so a producer that outruns the connection just grows this number.
    /// </summary>
    public ValueTask<long> GetBufferedAmount() => _js.Invoke<long>("BitButil.webRtc.channelBuffered", Id);

    /// <summary>
    /// Closes the channel. Idempotent, and safe during teardown. The connection itself stays open.
    /// </summary>
    /// <remarks>
    /// The <c>onClose</c> callback still runs after this - closing locally and being closed by the
    /// peer are the same event.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_closed) return;
        _closed = true;

        // The close event is asynchronous, and it is what runs the onClose callback and unregisters
        // the handlers (see WebSocketHandle, which closes for the same reason). Forgetting them here
        // would make disposal the one close nobody hears about - so they are only dropped by hand
        // when the close cannot be asked for at all.
        try { await _js.InvokeVoid("BitButil.webRtc.closeChannel", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) // teardown: circuit gone, cancelled, or already disposed
        {
            _owner.ForgetChannel(Id);
        }
    }
}
