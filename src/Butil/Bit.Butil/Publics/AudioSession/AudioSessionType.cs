namespace Bit.Butil;

/// <summary>
/// What the page's audio is for - the declaration the operating system routes and mixes by.
/// </summary>
/// <remarks>
/// The type decides whether other apps get ducked, whether this audio survives the phone's silent
/// switch, and what happens when a call comes in. Getting it wrong is what makes a game's
/// background music silence someone's podcast.
/// </remarks>
public enum AudioSessionType
{
    /// <summary>Unknown or unset - what a runtime without the API reports.</summary>
    Unknown,

    /// <summary>Let the browser decide from what the page is doing. The default.</summary>
    Auto,

    /// <summary>
    /// Content the user came for: a video, a track, a podcast. Interrupts other audio, keeps playing
    /// with the ringer switch off, and shows up in the OS media controls.
    /// </summary>
    Playback,

    /// <summary>A short sound over other audio - a notification chime. Ducks other audio briefly.</summary>
    Transient,

    /// <summary>A short sound that must be heard alone - a turn-by-turn instruction. Pauses other audio.</summary>
    TransientSolo,

    /// <summary>
    /// Incidental sound: UI clicks, game effects. Mixes with other audio and obeys the silent switch,
    /// so it never interrupts someone's music.
    /// </summary>
    Ambient,

    /// <summary>Simultaneous playback and capture - a call, or a recording app with monitoring.</summary>
    PlayAndRecord,
}
