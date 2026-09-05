namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/BaseAudioContext/state">AudioContext.state</see>.
/// </summary>
public enum AudioContextState
{
    /// <summary>
    /// Not processing audio. Every context starts here under an autoplay policy, and stays here until
    /// <see cref="WebAudio.Resume"/> is called from a user gesture - the single most common reason
    /// "nothing plays".
    /// </summary>
    Suspended,

    /// <summary>Processing audio normally.</summary>
    Running,

    /// <summary>Closed for good. Nothing can be created on it any more.</summary>
    Closed
}
