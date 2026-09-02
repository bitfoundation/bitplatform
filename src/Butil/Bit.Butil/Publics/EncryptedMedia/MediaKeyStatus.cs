namespace Bit.Butil;

/// <summary>
/// What a key can currently do, from
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeySession/keyStatuses">MediaKeySession.keyStatuses</see>.
/// </summary>
/// <remarks>
/// Only <see cref="Usable"/> decrypts. The rest are the reasons a licensed stream still shows a
/// black frame, and telling them apart is what lets a player react correctly - fetch a new licence,
/// drop to a lower rung, or tell the user their display is the problem.
/// </remarks>
public enum MediaKeyStatus
{
    /// <summary>The key decrypts content right now.</summary>
    Usable,

    /// <summary>The licence's expiry has passed. A new licence is needed.</summary>
    Expired,

    /// <summary>The licence was released - the terminal state of <see cref="MediaKeySessionHandle.Remove"/>.</summary>
    Released,

    /// <summary>
    /// The key exists but the output path won't take it: no HDCP on the connected display, or a
    /// mirrored screen. Playing a lower-robustness rendition is the usual fallback.
    /// </summary>
    OutputRestricted,

    /// <summary>The key is usable only at a reduced resolution, for the same output-protection reasons.</summary>
    OutputDownscaled,

    /// <summary>The key system hasn't decided yet - a status that normally resolves within moments.</summary>
    StatusPending,

    /// <summary>The key system hit an error with this key and it will not become usable.</summary>
    InternalError,

    /// <summary>A status this version of Butil doesn't know.</summary>
    Unknown
}
