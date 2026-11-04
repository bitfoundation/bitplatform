using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Vertical stripes across the content pane, one per time unit, cycling
/// <see cref="Colors"/>. Two colors with the default unit
/// (<see cref="BitResourceTimelineSurfaceUnit.Day"/>) alternate calendar days
/// for the whole range: the same local day keeps its color wherever the loaded
/// window sits.
/// </summary>
public class BitResourceTimelineColumnStripes
{
    /// <summary>
    /// What one stripe covers. <c>null</c> is a day.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineSurfaceUnit? Unit { get; set; }

    /// <summary>
    /// Whether the cycle runs continuously across the range or restarts each
    /// day or week. <c>null</c> is continuous.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineStripeAlign? Align { get; set; }

    /// <summary>
    /// How many units one color covers before the next. <c>null</c> or values
    /// below 1 are 1, so two colors alternate every day (or hour, or week).
    /// An hour pattern with three colors and a span of 8 paints three 8-hour
    /// shifts.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Span { get; set; }

    /// <summary>
    /// Added to the stripe index before the color is chosen, so the cycle can
    /// start on another color without reordering <see cref="Colors"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Offset { get; set; }

    /// <summary>
    /// CSS colors cycled in order (<c>"#f1f3f5"</c>, <c>"rgba(0,0,0,.04)"</c>).
    /// At least one is required; a single color paints every stripe the same.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Colors { get; set; }
}
