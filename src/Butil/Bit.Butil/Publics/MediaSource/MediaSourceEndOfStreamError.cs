namespace Bit.Butil;

/// <summary>
/// Why a stream ended, the optional argument of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource/endOfStream">MediaSource.endOfStream()</see>.
/// </summary>
public enum MediaSourceEndOfStreamError
{
    /// <summary>
    /// A clean end: everything that was meant to play has been appended. The element fires
    /// <c>ended</c> when it reaches it.
    /// </summary>
    None,

    /// <summary>
    /// The app could not fetch the next segment. The element ends in an error state, which is what
    /// distinguishes a failed stream from a finished one for anything watching the element.
    /// </summary>
    Network,

    /// <summary>The bytes that were appended could not be decoded.</summary>
    Decode
}
