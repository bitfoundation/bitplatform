namespace Bit.Butil;

/// <summary>
/// A platform control that can drive playback, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSessionActionDetails#action">MediaSessionAction</see>.
/// </summary>
/// <remarks>
/// Engines implement different subsets, and registering a handler is also what makes the
/// corresponding control appear. <see cref="MediaSession.SetActionHandler"/> returns false for the
/// ones this engine doesn't know.
/// </remarks>
public enum MediaSessionAction
{
    Play,
    Pause,
    Stop,

    /// <summary>Jump back by the details' seek offset, or a sensible default when none is given.</summary>
    SeekBackward,

    /// <summary>Jump forward by the details' seek offset, or a sensible default when none is given.</summary>
    SeekForward,

    /// <summary>Jump to an absolute position - the details carry the target time.</summary>
    SeekTo,

    PreviousTrack,
    NextTrack,

    /// <summary>Skip an advertisement.</summary>
    SkipAd,

    /// <summary>Mute/unmute the microphone. Meant for conferencing apps.</summary>
    ToggleMicrophone,

    /// <summary>Turn the camera on/off. Meant for conferencing apps.</summary>
    ToggleCamera,

    /// <summary>End a call. Meant for conferencing apps.</summary>
    HangUp,
}
