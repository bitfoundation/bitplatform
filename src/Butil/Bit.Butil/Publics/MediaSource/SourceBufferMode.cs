namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/mode">SourceBuffer.mode</see>:
/// how the timestamps inside an appended segment are interpreted.
/// </summary>
public enum SourceBufferMode
{
    /// <summary>
    /// The segment's own timestamps place it on the timeline - the default for a stream whose
    /// segments carry absolute presentation times, which is what a DASH or HLS ladder produces.
    /// </summary>
    Segments,

    /// <summary>
    /// Each appended segment is placed immediately after the previous one, its internal timestamps
    /// ignored. What you want when splicing independently produced pieces (an ad break, a
    /// concatenation) whose timelines don't line up.
    /// </summary>
    Sequence
}
