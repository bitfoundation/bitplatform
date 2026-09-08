namespace Bit.Butil;

/// <summary>
/// A STUN or TURN server for <see cref="WebRtc.CreatePeerConnection"/>.
/// </summary>
/// <remarks>
/// A <b>STUN</b> server answers one question - "what does my address look like from outside?" - and
/// costs nothing to run, which is why public ones exist. A <b>TURN</b> server <em>relays</em> the
/// whole conversation when no direct path can be found, which costs bandwidth, needs credentials,
/// and is the reason a WebRTC feature has an infrastructure bill. Somewhere between a tenth and a
/// fifth of connections need one.
/// </remarks>
public class RtcIceServer
{
    /// <summary>
    /// The server's URLs: <c>"stun:stun.example.com:3478"</c>, or one or more <c>turn:</c> /
    /// <c>turns:</c> URLs for the same server. Several here are alternatives for one server, not a
    /// list of different ones.
    /// </summary>
    public string[] Urls { get; set; } = [];

    /// <summary>The username, for TURN. Ignored for STUN, which is unauthenticated.</summary>
    public string? Username { get; set; }

    /// <summary>
    /// The credential, for TURN. It reaches the browser, so it reaches the user: issue short-lived
    /// per-session credentials from your server rather than shipping a shared secret.
    /// </summary>
    public string? Credential { get; set; }
}
