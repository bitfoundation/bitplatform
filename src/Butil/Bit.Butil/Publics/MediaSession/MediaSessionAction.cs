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

    /// <summary>Go to the previous slide. Meant for presentation apps.</summary>
    PreviousSlide,

    /// <summary>Go to the next slide. Meant for presentation apps.</summary>
    NextSlide,

    /// <summary>
    /// Move the playing video into a picture-in-picture window. The platform offers it when the user
    /// switches away, which is the one moment the page may open the window without a click of its own.
    /// </summary>
    EnterPictureInPicture,

    /// <summary>
    /// The user started speaking while muted. Meant for conferencing apps, which can use it to
    /// suggest unmuting. Voice detection only runs while the page holds the microphone.
    /// </summary>
    VoiceActivity,
}
