using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Horizontal stripes, one per visible resource row, cycling <see cref="Colors"/>
/// down the row list. The index is the row's place among the rows currently
/// shown, so collapsing a group above re-stripes the rows under it; scrolling
/// does not. A resource's own background replaces the stripe on that row.
/// </summary>
public class BitResourceTimelineRowStripes
{
    /// <summary>
    /// CSS colors cycled in order. Two colors are a zebra. At least one is required.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Colors { get; set; }

    /// <summary>
    /// How many rows one color covers before the next. <c>null</c> or values
    /// below 1 are 1.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Span { get; set; }

    /// <summary>Added to the row index before the color is chosen.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Offset { get; set; }
}
