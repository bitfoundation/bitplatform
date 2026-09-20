namespace Bit.Butil;

/// <summary>An offer or an answer - the text one peer sends the other to describe what it will send and receive.</summary>
/// <param name="Type"><c>"offer"</c> or <c>"answer"</c>. Null when the description could not be made.</param>
/// <param name="Sdp">
/// The session description itself. Opaque text: pass it to the peer unchanged, and hand what comes
/// back to <see cref="PeerConnectionHandle.SetRemoteDescription"/>. Editing it by hand is a way to
/// force a codec, and also a way to produce a connection that never forms.
/// </param>
/// <param name="Error">Why it could not be made, or null. Creating an answer with no offer outstanding is the usual cause.</param>
public record RtcSessionDescription(string? Type, string? Sdp, string? Error);
