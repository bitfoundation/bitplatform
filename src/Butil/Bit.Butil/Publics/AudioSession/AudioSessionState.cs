namespace Bit.Butil;

/// <summary>
/// Whether the page's audio session currently holds the audio focus.
/// </summary>
public enum AudioSessionState
{
    /// <summary>Unknown - what a runtime without the API reports.</summary>
    Unknown,

    /// <summary>Nothing is playing, and nothing has been claimed.</summary>
    Inactive,

    /// <summary>Playing, with the focus this session's type asked for.</summary>
    Active,

    /// <summary>
    /// Something else took the audio - a phone call, another app. Playback has stopped and will not
    /// resume on its own; the session returns to <see cref="Active"/> when the interruption ends.
    /// </summary>
    Interrupted,
}
