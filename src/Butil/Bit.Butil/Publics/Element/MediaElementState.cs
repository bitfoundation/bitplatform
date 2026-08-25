namespace Bit.Butil;

/// <summary>
/// A snapshot of an <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element's playback state, read in
/// one round trip by <see cref="ElementReferenceMediaExtensions.GetMediaState"/>.
/// </summary>
public class MediaElementState
{
    public bool Paused { get; set; }

    /// <summary>True once playback has run past the end (and looping is off).</summary>
    public bool Ended { get; set; }

    /// <summary>True while a seek is in flight.</summary>
    public bool Seeking { get; set; }

    public bool Muted { get; set; }

    public bool Loop { get; set; }

    public bool Autoplay { get; set; }

    /// <summary>0 to 1. Always 1 on iOS, where volume is a hardware control.</summary>
    public double Volume { get; set; }

    /// <summary>1 is normal speed.</summary>
    public double PlaybackRate { get; set; }

    /// <summary>The current position in seconds.</summary>
    public double CurrentTime { get; set; }

    /// <summary>
    /// The total length in seconds, or 0 when it isn't a finite number - before metadata loads, and
    /// for a live stream. Check <see cref="ReadyState"/> to tell those two apart from a genuinely
    /// zero-length source.
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// How much of the media is available: 0 nothing, 1 metadata, 2 current frame, 3 enough to play
    /// forward a little, 4 enough to play through.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLMediaElement/readyState">HTMLMediaElement.readyState</see>
    /// </summary>
    public int ReadyState { get; set; }

    /// <summary>
    /// 0 empty, 1 idle, 2 loading, 3 no source.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLMediaElement/networkState">HTMLMediaElement.networkState</see>
    /// </summary>
    public int NetworkState { get; set; }

    /// <summary>The URL the element actually resolved and is playing. Empty when there is none.</summary>
    public string CurrentSrc { get; set; } = string.Empty;

    /// <summary>
    /// The end of the last buffered range, in seconds - how far ahead of
    /// <see cref="CurrentTime"/> the browser has data, and what a buffering indicator draws.
    /// </summary>
    public double BufferedEnd { get; set; }

    /// <summary>The video's intrinsic width, or 0 for audio and before metadata loads.</summary>
    public int VideoWidth { get; set; }

    /// <summary>The video's intrinsic height, or 0 for audio and before metadata loads.</summary>
    public int VideoHeight { get; set; }
}
