using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A connection to one peer, from <see cref="WebRtc.CreatePeerConnection"/>.
/// </summary>
/// <remarks>
/// The handshake, in order: one side calls <see cref="CreateOffer"/> and
/// <see cref="SetLocalDescription"/>, sends the offer over your signalling channel; the other calls
/// <see cref="SetRemoteDescription"/>, <see cref="CreateAnswer"/> and
/// <see cref="SetLocalDescription"/>, and sends the answer back. Meanwhile both sides send every ICE
/// candidate they discover to the other, which passes them to <see cref="AddIceCandidate"/>.
/// <br/>
/// Everything a peer will send has to be added <em>before</em> the offer that describes it - a data
/// channel, a media track. Adding one afterwards means negotiating again.
/// </remarks>
public sealed class PeerConnectionHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly WebRtc _owner;
    private bool _closed;

    internal PeerConnectionHandle(IJSRuntime js, WebRtc owner, Guid id)
    {
        _js = js;
        _owner = owner;
        Id = id;
    }

    /// <summary>The internal connection id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// Opens a data channel to the peer.
    /// </summary>
    /// <param name="label">A name both sides see. It identifies the channel when several are open.</param>
    /// <param name="ordered">
    /// True (the default) to deliver messages in the order they were sent. False allows a later
    /// message to arrive first, which is what a game or a live cursor wants - being current matters
    /// more than being in order.
    /// </param>
    /// <param name="maxRetransmits">
    /// How many times to retry a lost message before giving up on it, or -1 (the default) for
    /// reliable delivery with no limit. Zero means never retry - the lowest-latency,
    /// least-guaranteed mode there is, and something TCP cannot do at all.
    /// </param>
    /// <returns>A handle, or null when the connection is closed.</returns>
    /// <remarks>
    /// Create it before the offer. A channel added afterwards changes what the connection carries,
    /// which means another offer and answer.
    /// </remarks>
    public async ValueTask<RtcDataChannelHandle?> CreateDataChannel(string label, bool ordered = true, int maxRetransmits = -1)
    {
        ArgumentNullException.ThrowIfNull(label);

        var channelId = Guid.NewGuid();
        var created = await _js.Invoke<bool>("BitButil.webRtc.createChannel",
            _owner.DotNetRef, Id, channelId, label, ordered, maxRetransmits);

        if (created is false) return null;

        return new RtcDataChannelHandle(_js, _owner, channelId, label);
    }

    /// <summary>
    /// Describes what this side wants to send and receive.
    /// </summary>
    /// <returns>The offer, or one carrying <see cref="RtcSessionDescription.Error"/> when it could not be made.</returns>
    /// <remarks>
    /// The offer is text. Getting it to the other peer is your problem, not the API's.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RtcSessionDescription))]
    public async ValueTask<RtcSessionDescription?> CreateOffer()
        => await _js.Invoke<RtcSessionDescription?>("BitButil.webRtc.createOffer", Id);

    /// <summary>
    /// Answers an offer that has already been given to <see cref="SetRemoteDescription"/>.
    /// </summary>
    /// <returns>The answer, or one carrying <see cref="RtcSessionDescription.Error"/> - most often because no offer has been set.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RtcSessionDescription))]
    public async ValueTask<RtcSessionDescription?> CreateAnswer()
        => await _js.Invoke<RtcSessionDescription?>("BitButil.webRtc.createAnswer", Id);

    /// <summary>
    /// Applies this side's own offer or answer, which is what starts ICE gathering.
    /// </summary>
    /// <returns>Null on success, or the reason it failed.</returns>
    public ValueTask<string?> SetLocalDescription(RtcSessionDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        return _js.Invoke<string?>("BitButil.webRtc.setLocalDescription", Id, description.Type, description.Sdp);
    }

    /// <summary>
    /// Applies the description that arrived from the peer.
    /// </summary>
    /// <returns>Null on success, or the reason it failed - an answer applied while no offer is outstanding, most often.</returns>
    public ValueTask<string?> SetRemoteDescription(RtcSessionDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        return _js.Invoke<string?>("BitButil.webRtc.setRemoteDescription", Id, description.Type, description.Sdp);
    }

    /// <summary>
    /// Adds a candidate the peer discovered.
    /// </summary>
    /// <param name="candidateJson">
    /// The JSON your <c>onIceCandidate</c> callback handed you on the other side - or null, which is
    /// how that side says it has no more.
    /// </param>
    /// <returns>Null on success, or the reason it failed.</returns>
    /// <remarks>
    /// Candidates keep arriving after the offer and answer are done. Passing each one on as it
    /// arrives is what makes a connection establish quickly.
    /// </remarks>
    public ValueTask<string?> AddIceCandidate(string? candidateJson)
        => _js.Invoke<string?>("BitButil.webRtc.addIceCandidate", Id, candidateJson);

    /// <summary>
    /// Sends every track of a <see cref="MediaDevices"/> stream to the peer.
    /// </summary>
    /// <returns>False when the connection is closed or the stream has been released.</returns>
    /// <remarks>
    /// Add the tracks before the offer: the offer describes what will be sent, so adding a camera
    /// afterwards means negotiating again.
    /// </remarks>
    public ValueTask<bool> AddTracksFrom(MediaStreamHandle stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return _js.Invoke<bool>("BitButil.webRtc.addTracks", Id, stream.Id);
    }

    /// <summary>
    /// Shows what the peer is sending in a <c>&lt;video&gt;</c> or <c>&lt;audio&gt;</c> element.
    /// </summary>
    /// <returns>False when the connection is closed.</returns>
    /// <remarks>
    /// Safe to call before any track has arrived - the element is attached to a stream that fills in
    /// as tracks come, which saves waiting for the right moment.
    /// </remarks>
    public ValueTask<bool> AttachRemoteMedia(ElementReference videoOrAudioElement)
        => _js.Invoke<bool>("BitButil.webRtc.attachRemote", Id, videoOrAudioElement);

    /// <summary>
    /// The overall state: <c>"new"</c>, <c>"connecting"</c>, <c>"connected"</c>,
    /// <c>"disconnected"</c>, <c>"failed"</c> or <c>"closed"</c>.
    /// </summary>
    public ValueTask<string> GetConnectionState() => _js.Invoke<string>("BitButil.webRtc.connectionState", Id);

    /// <summary>
    /// The ICE agent's own state, which is finer-grained and often more useful while diagnosing why
    /// a connection is not forming.
    /// </summary>
    public ValueTask<string> GetIceConnectionState() => _js.Invoke<string>("BitButil.webRtc.iceConnectionState", Id);

    /// <summary>
    /// Where the offer/answer exchange has got to: <c>"stable"</c>, <c>"have-local-offer"</c>,
    /// <c>"have-remote-offer"</c> and the rest. <c>"stable"</c> before an offer means nothing has
    /// started; <c>"stable"</c> after an answer means the exchange is done.
    /// </summary>
    public ValueTask<string> GetSignalingState() => _js.Invoke<string>("BitButil.webRtc.signalingState", Id);

    /// <summary>
    /// Everything the browser knows about this connection: bitrates, packet loss, round-trip time,
    /// the codec in use, which candidate pair won.
    /// </summary>
    /// <returns>The report, flattened to strings.</returns>
    /// <remarks>
    /// The report's members differ per stat type, and a C# record could not describe them honestly -
    /// so each entry carries its <see cref="RtcStat.Type"/> and a dictionary. The two entries worth
    /// looking at first are usually <c>"candidate-pair"</c> with <c>nominated</c> true (the path in
    /// use, and its round-trip time) and the <c>"inbound-rtp"</c> entries (what is actually arriving).
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RtcStat))]
    public ValueTask<RtcStat[]> GetStats() => _js.Invoke<RtcStat[]>("BitButil.webRtc.stats", Id);

    /// <summary>
    /// Closes the connection. Idempotent, and safe during teardown.
    /// </summary>
    /// <remarks>
    /// A connection that is not closed keeps media flowing - and keeps the peer's side alive too,
    /// which is a camera light staying on somebody else's machine.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_closed) return;
        _closed = true;
        _owner.ForgetPeer(Id);

        try { await _js.InvokeVoid("BitButil.webRtc.close", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
