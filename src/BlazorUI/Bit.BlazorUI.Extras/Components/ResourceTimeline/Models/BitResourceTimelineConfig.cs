namespace Bit.BlazorUI;

/// <summary>
/// Bundles everything the timeline component needs to render: the resource rows
/// shown along the vertical axis, the time window shown along the horizontal
/// axis, and the allocation bars drawn within it. Assign a new instance to the
/// component's <c>Config</c> parameter to (re)render the timeline.
/// </summary>
public class BitResourceTimelineConfig
{
    /// <summary>Resource rows rendered along the vertical axis.</summary>
    public required List<BitResourceTimelineResource> Resources { get; set; }

    /// <summary>Start of the time window rendered along the horizontal axis.</summary>
    public required DateTimeOffset StartDate { get; set; }

    /// <summary>End of the time window rendered along the horizontal axis.</summary>
    public required DateTimeOffset EndDate { get; set; }

    /// <summary>Allocation bars to display.</summary>
    public required List<BitResourceTimelineAllocation> Allocations { get; set; }
}
