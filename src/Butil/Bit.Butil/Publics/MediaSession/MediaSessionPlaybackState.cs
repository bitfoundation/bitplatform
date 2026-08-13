namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSession/playbackState">MediaSession.playbackState</see>.
/// </summary>
public enum MediaSessionPlaybackState
{
    /// <summary>Nothing is loaded - the platform shows no transport controls.</summary>
    None,

    /// <summary>Loaded but not running; the platform shows a play button.</summary>
    Paused,

    /// <summary>Running; the platform shows a pause button.</summary>
    Playing,
}
