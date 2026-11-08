using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// An additional bar drawn immediately before (start) or after (end) a main
/// <see cref="BitResourceTimelineAllocation"/> bar. Edge bars share the main bar's row, height and
/// vertical position, differing only in their color and extent. They are purely
/// decorative (not selectable) and are typically used to visualize delays in
/// using the planned time of a resource.
/// </summary>
public class BitResourceTimelineEdgeBar
{
    /// <summary>
    /// Length of the edge bar. A start edge bar extends backwards from the main
    /// bar's start; an end edge bar extends forwards from the main bar's end.
    /// Durations of zero or less are not drawn.
    /// </summary>
    [JsonConverter(typeof(BitResourceTimelineMillisecondsTimeSpanConverter))]
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// CSS color for the edge bar fill (for example <c>"red"</c> or <c>"#e03131"</c>).
    /// Falls back to the renderer's default bar color when null or empty.
    /// </summary>
    public string? Color { get; set; }
}
