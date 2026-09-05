namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource/readyState">MediaSource.readyState</see>.
/// </summary>
public enum MediaSourceReadyState
{
    /// <summary>Not attached to a media element, or detached again - nothing can be appended.</summary>
    Closed,

    /// <summary>Attached and accepting buffers and appends. The state a handle is handed out in.</summary>
    Open,

    /// <summary>Attached, but <see cref="MediaSourceHandle.EndOfStream"/> was called - playback runs to the end of what was appended.</summary>
    Ended
}
