namespace Bit.Butil;

/// <summary>
/// One frame's worth of pointer input: the event the browser delivered, every sample it merged into
/// that event, and where it thinks the pointer is going next.
/// </summary>
/// <remarks>
/// The browser delivers at most one pointer event per animation frame, but a pen or a high-rate
/// mouse produces several samples in that time. Drawing from <see cref="Current"/> alone throws the
/// rest away, which is why a fast stroke comes out as straight segments -
/// <see cref="Coalesced"/> is the fix.
/// </remarks>
public class PointerFrame
{
    /// <summary>The DOM event type this frame came from - <c>"pointermove"</c>, <c>"pointerdown"</c>, and so on.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>The pointer's id, stable for the life of a contact - use it to keep two fingers apart.</summary>
    public int PointerId { get; set; }

    /// <summary>What is pointing: <c>"mouse"</c>, <c>"pen"</c> or <c>"touch"</c>.</summary>
    public string PointerType { get; set; } = string.Empty;

    /// <summary>True for the primary pointer of its type - the first finger of a multi-touch gesture.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Bitmask of the buttons currently held.</summary>
    public int Buttons { get; set; }

    /// <summary>The event as delivered - the same thing as the last entry of <see cref="Coalesced"/>.</summary>
    public PointerSample Current { get; set; } = new();

    /// <summary>
    /// Every sample the browser merged into this frame, oldest first. Draw through all of them
    /// rather than only <see cref="Current"/>.
    /// </summary>
    public PointerSample[] Coalesced { get; set; } = [];

    /// <summary>
    /// Where the browser predicts the pointer is heading, oldest first. Empty unless prediction was
    /// asked for, and empty on runtimes without it.
    /// </summary>
    /// <remarks>
    /// These are guesses, and they are wrong often enough to matter. The usual use is to draw them
    /// as a provisional "ink ahead" segment that is thrown away and redrawn on the next real frame -
    /// it removes perceived latency without ever committing a predicted point to the document.
    /// </remarks>
    public PointerSample[] Predicted { get; set; } = [];
}
