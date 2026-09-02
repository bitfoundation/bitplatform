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
    /// <summary>Resume playback.</summary>
    Play,

    /// <summary>Pause playback, keeping the position.</summary>
    Pause,

    /// <summary>Stop playback and give up the session.</summary>
    Stop,

    /// <summary>Jump back by the details' seek offset, or a sensible default when none is given.</summary>
    SeekBackward,

    /// <summary>Jump forward by the details' seek offset, or a sensible default when none is given.</summary>
    SeekForward,

    /// <summary>Jump to an absolute position - the details carry the target time.</summary>
    SeekTo,

    /// <summary>Go to the previous track.</summary>
    PreviousTrack,
    
    /// <summary>Go to the next track.</summary>
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
