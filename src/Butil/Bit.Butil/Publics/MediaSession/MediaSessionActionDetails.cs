namespace Bit.Butil;

/// <summary>
/// What the platform passed along with an action, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSessionActionDetails">MediaSessionActionDetails</see>.
/// </summary>
public class MediaSessionActionDetails
{
    /// <summary>The action name as the platform reported it, e.g. <c>"seekto"</c>.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// For <see cref="MediaSessionAction.SeekTo"/>: the absolute position to move to, in seconds.
    /// Null for every other action.
    /// </summary>
    public double? SeekTime { get; set; }

    /// <summary>
    /// For the seek-by actions: how far to move, in seconds. Null when the platform didn't specify
    /// one, in which case pick your own step (a few seconds is conventional).
    /// </summary>
    public double? SeekOffset { get; set; }

    /// <summary>
    /// True when the platform wants a fast, possibly inaccurate seek - it is scrubbing, and a
    /// precise seek per step would be wasted work.
    /// </summary>
    public bool FastSeek { get; set; }
}
