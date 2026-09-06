using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/RTCPeerConnection">RTCPeerConnection</see>
/// and <see href="https://developer.mozilla.org/en-US/docs/Web/API/RTCDataChannel">RTCDataChannel</see>:
/// media and data sent directly between two browsers.
/// </summary>
/// <remarks>
/// <see cref="MediaDevices"/> acquires the stream and <see cref="MediaRecorder"/> writes it to a
/// file; this is what sends it to somebody else. It also carries arbitrary data, which is what a
/// <see cref="RtcDataChannelHandle"/> is - a socket to a peer rather than to a server, with
/// optional unreliable delivery that no TCP-based transport can offer.
/// <br/>
/// <b>WebRTC does not connect anyone by itself.</b> Two peers cannot find each other without a
/// channel that already works: an offer, an answer and a stream of ICE candidates have to be carried
/// between them by something else - your own server over
/// <see cref="WebSocket"/> or <see cref="EventSource"/>, a shared document, a QR code. That
/// exchange is <em>signalling</em>, it is not part of the API, and it is the part people are
/// surprised to have to build.
/// <br/>
/// Reaching a peer across the internet usually also needs a STUN server to discover the public
/// address, and a TURN server to relay when a direct path cannot be found at all. Both are
/// infrastructure you supply through <see cref="RtcIceServer"/>.
/// </remarks>
[ButilService(typeof(WebRtc))]
public class WebRtc(IJSRuntime js) : IAsyncDisposable
{
    internal const string IceCandidateMethodName = nameof(InvokeIceCandidate);
    internal const string ConnectionStateMethodName = nameof(InvokeConnectionState);
    internal const string TrackMethodName = nameof(InvokeTrack);
    internal const string RemoteChannelMethodName = nameof(InvokeRemoteChannel);
    internal const string ChannelOpenMethodName = nameof(InvokeChannelOpen);
    internal const string ChannelCloseMethodName = nameof(InvokeChannelClose);
    internal const string ChannelMessageMethodName = nameof(InvokeChannelMessage);

    private readonly ConcurrentDictionary<Guid, PeerHandlers> _peers = new();
    private readonly ConcurrentDictionary<Guid, ChannelHandlers> _channels = new();

    // Per-instance callback reference (see Keyboard): connections are isolated per circuit / WASM
    // app and closed on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<WebRtc>? _dotNetRef;
    internal DotNetObjectReference<WebRtc> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    private sealed class PeerHandlers
    {
        public Action<string?>? OnIceCandidate;
        public Action<string>? OnConnectionState;
        public Action<string>? OnTrack;
        public Action<RtcDataChannelHandle>? OnRemoteChannel;
    }

    private sealed class ChannelHandlers
    {
        public Action? OnOpen;
        public Action? OnClose;
        public Action<ButilMessage>? OnMessage;
    }

    /// <summary>True when the runtime exposes <c>RTCPeerConnection</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webRtc.isSupported");

    /// <summary>
    /// Invoked from JS for each ICE candidate. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(IceCandidateMethodName)]
    public void InvokeIceCandidate(Guid id, string? candidateJson)
    {
        if (_peers.TryGetValue(id, out var handlers)) handlers.OnIceCandidate?.Invoke(candidateJson);
    }

    /// <summary>
    /// Invoked from JS when the connection state changes. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ConnectionStateMethodName)]
    public void InvokeConnectionState(Guid id, string state)
    {
        if (_peers.TryGetValue(id, out var handlers)) handlers.OnConnectionState?.Invoke(state);
    }

    /// <summary>
    /// Invoked from JS when a remote track arrives. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(TrackMethodName)]
    public void InvokeTrack(Guid id, string kind)
    {
        if (_peers.TryGetValue(id, out var handlers)) handlers.OnTrack?.Invoke(kind);
    }

    /// <summary>
    /// Invoked from JS when the peer opens a channel. Public + <see cref="JSInvokableAttribute"/> so
    /// it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(RemoteChannelMethodName)]
    public void InvokeRemoteChannel(Guid peerId, Guid channelId, string label)
    {
        if (_peers.TryGetValue(peerId, out var handlers) is false) return;

        handlers.OnRemoteChannel?.Invoke(new RtcDataChannelHandle(js, this, channelId, label));
    }

    /// <summary>
    /// Invoked from JS when a channel opens. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ChannelOpenMethodName)]
    public void InvokeChannelOpen(Guid channelId)
    {
        if (_channels.TryGetValue(channelId, out var handlers)) handlers.OnOpen?.Invoke();
    }

    /// <summary>
    /// Invoked from JS when a channel closes. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ChannelCloseMethodName)]
    public void InvokeChannelClose(Guid channelId)
    {
        // The channel is gone: drop its handlers with it rather than waiting for a Dispose the
        // caller has no reason to make.
        if (_channels.TryRemove(channelId, out var handlers)) handlers.OnClose?.Invoke();
    }

    /// <summary>
    /// Invoked from JS for each channel message. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ChannelMessageMethodName)]
    public void InvokeChannelMessage(Guid channelId, bool isBinary, string? text, byte[]? data)
    {
        if (_channels.TryGetValue(channelId, out var handlers))
            handlers.OnMessage?.Invoke(new ButilMessage(isBinary, text, data));
    }

    /// <summary>
    /// Creates a peer connection.
    /// </summary>
    /// <param name="iceServers">
    /// STUN servers to discover this machine's public address, and TURN servers to relay when no
    /// direct path exists. Two peers on the same network can connect with none; anything across the
    /// internet usually needs at least STUN, and a meaningful share of connections need TURN.
    /// </param>
    /// <param name="onIceCandidate">
    /// Called for every candidate as it is discovered, with the JSON to send to the other peer - and
    /// once with null, meaning gathering is finished. Send each one over your signalling channel as
    /// it arrives; waiting for them all is slower and gains nothing.
    /// </param>
    /// <param name="onConnectionState">
    /// Called with <c>"new"</c>, <c>"connecting"</c>, <c>"connected"</c>, <c>"disconnected"</c>,
    /// <c>"failed"</c> or <c>"closed"</c>. <c>"disconnected"</c> often recovers by itself;
    /// <c>"failed"</c> does not, and means renegotiating or giving up.
    /// </param>
    /// <param name="onTrack">Called with <c>"audio"</c> or <c>"video"</c> as the peer's tracks arrive. Attach them with <see cref="PeerConnectionHandle.AttachRemoteMedia"/>.</param>
    /// <param name="onRemoteChannel">
    /// Called when the <em>other</em> side opens a data channel - which arrives as an event rather
    /// than as a return value. Call <see cref="RtcDataChannelHandle.Listen"/> on it here, before
    /// returning: the channel is already open, and its events are held only until this callback
    /// returns - which is what keeps the open, and the peer's first messages, from being lost to the
    /// round trip that delivered the channel. Anything attached after an <c>await</c> is too late.
    /// </param>
    /// <returns>A handle, or null when the runtime has no <c>RTCPeerConnection</c>.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RtcIceServer))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilMessage))]
    public async ValueTask<PeerConnectionHandle?> CreatePeerConnection(RtcIceServer[]? iceServers = null,
                                                                       Action<string?>? onIceCandidate = null,
                                                                       Action<string>? onConnectionState = null,
                                                                       Action<string>? onTrack = null,
                                                                       Action<RtcDataChannelHandle>? onRemoteChannel = null)
    {
        var id = Guid.NewGuid();
        _peers[id] = new PeerHandlers
        {
            OnIceCandidate = onIceCandidate,
            OnConnectionState = onConnectionState,
            OnTrack = onTrack,
            OnRemoteChannel = onRemoteChannel
        };

        var created = await js.Invoke<bool>("BitButil.webRtc.create", DotNetRef, id, iceServers ?? []);

        if (created is false)
        {
            _peers.TryRemove(id, out _);
            return null;
        }

        return new PeerConnectionHandle(js, this, id);
    }

    internal void ForgetPeer(Guid id) => _peers.TryRemove(id, out _);

    internal void ForgetChannel(Guid id) => _channels.TryRemove(id, out _);

    // The only place a channel's entry is created. An entry whose callbacks are all null behaves
    // exactly like no entry at all, so pre-seeding one when the channel is made would be state with
    // nothing to observe it and one more place to keep in step with this one.
    internal void SetChannelHandlers(Guid channelId, Action? onOpen, Action? onClose, Action<ButilMessage>? onMessage)
    {
        var handlers = _channels.GetOrAdd(channelId, static _ => new ChannelHandlers());
        if (onOpen is not null) handlers.OnOpen = onOpen;
        if (onClose is not null) handlers.OnClose = onClose;
        if (onMessage is not null) handlers.OnMessage = onMessage;
    }

    /// <summary>
    /// On scope/circuit teardown, closes every connection and channel whose handle was never
    /// disposed - a peer connection that is not closed keeps media flowing and keeps the peer's
    /// connection alive too.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _peers.Clear();
            _channels.Clear();
            await js.InvokeVoid("BitButil.webRtc.disposeAll");
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
