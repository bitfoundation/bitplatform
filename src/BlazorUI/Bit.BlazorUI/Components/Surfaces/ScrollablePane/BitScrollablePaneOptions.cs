namespace Bit.BlazorUI;

/// <summary>
/// Everything the browser side of a <see cref="BitScrollablePane"/> is driven with.
/// </summary>
/// <remarks>
/// It is handed over as a whole, both at setup and on every change, so the two sides can never end up
/// disagreeing about one of these while agreeing about the rest. Each flag says whether a piece of work
/// is worth doing at all: nothing is measured, observed or reported for a pane that asked for none of it.
/// <br />
/// It is a record so that "has any of this changed since the browser was last told about it" is the value
/// equality the compiler writes over every member, rather than a comparison of its own that a member added
/// here could be left out of.
/// </remarks>
internal record class BitScrollablePaneOptions
{
    /// <summary>
    /// Whether the edges of the pane are faded out while there is content beyond them.
    /// </summary>
    public bool Fade { get; set; }

    /// <summary>
    /// How near an edge (in pixels) counts as having reached it.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// The shortest interval (in milliseconds) between two scroll reports, or 0 for one report per frame.
    /// </summary>
    public int Throttle { get; set; }

    /// <summary>
    /// Whether the scroll position is reported back at all.
    /// </summary>
    public bool Scroll { get; set; }

    /// <summary>
    /// Whether the start of a scroll is reported back.
    /// </summary>
    public bool ScrollStart { get; set; }

    /// <summary>
    /// Whether the end of a scroll is reported back.
    /// </summary>
    public bool ScrollEnd { get; set; }

    /// <summary>
    /// Whether reaching the top of the content is reported back.
    /// </summary>
    public bool Top { get; set; }

    /// <summary>
    /// Whether reaching the bottom of the content is reported back.
    /// </summary>
    public bool Bottom { get; set; }

    /// <summary>
    /// Whether reaching the visual left edge of the content is reported back.
    /// </summary>
    public bool Left { get; set; }

    /// <summary>
    /// Whether reaching the visual right edge of the content is reported back.
    /// </summary>
    public bool Right { get; set; }

    /// <summary>
    /// Whether the pane keeps itself pinned to the end of its content as the content grows.
    /// </summary>
    public bool AutoScroll { get; set; }

    /// <summary>
    /// How near the end (in pixels) the pane has to be left for the pinning to keep going.
    /// </summary>
    public int AutoScrollThreshold { get; set; }

    /// <summary>
    /// Whether the moves the pane makes itself are animated.
    /// </summary>
    public bool Smooth { get; set; }

    /// <summary>
    /// Whether the pane can be scrolled by dragging its content with a pointer.
    /// </summary>
    public bool Drag { get; set; }

    /// <summary>
    /// Whether a released drag carries on at the speed it was let go at and slows to a stop.
    /// </summary>
    public bool Momentum { get; set; }

    /// <summary>
    /// Whether a vertical wheel over a pane that only scrolls sideways scrolls it sideways.
    /// </summary>
    public bool Wheel { get; set; }

    /// <summary>
    /// Whether the reader's place is kept when content lands above what they are looking at.
    /// </summary>
    public bool Preserve { get; set; }
}
