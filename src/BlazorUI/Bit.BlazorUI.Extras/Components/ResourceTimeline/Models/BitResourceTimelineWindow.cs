namespace Bit.BlazorUI;

/// <summary>
/// A time window the renderer asks the host to supply allocations for when the
/// timeline runs in on-demand (windowed) mode. The host returns every allocation
/// whose time span overlaps <see cref="Start"/>..<see cref="End"/>, across all
/// resources. The window is already buffered beyond the visible range, so it is
/// safe to return exactly the overlapping allocations.
/// </summary>
public sealed class BitResourceTimelineWindow
{
    /// <summary>Inclusive start of the requested window.</summary>
    public DateTimeOffset Start { get; init; }

    /// <summary>Inclusive end of the requested window.</summary>
    public DateTimeOffset End { get; init; }
}
