using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// One explicit rectangle on the content pane, painted above column and row
/// patterns. Without <see cref="ResourceId"/> it spans every visible row (a
/// column). With a resource id it spans only that row, for as long as the row
/// is visible — a collapsed row is not painted.
/// </summary>
public class BitResourceTimelineSurfaceBand
{
    /// <summary>Start of the band (inclusive).</summary>
    [JsonConverter(typeof(BitResourceTimelineUnixMillisecondsConverter))]
    public DateTimeOffset Start { get; set; }

    /// <summary>End of the band (exclusive). A band with <c>End</c> not after <see cref="Start"/> is not painted.</summary>
    [JsonConverter(typeof(BitResourceTimelineUnixMillisecondsConverter))]
    public DateTimeOffset End { get; set; }

    /// <summary>CSS color. A null or empty color is not painted.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; set; }

    /// <summary>
    /// When set, only this resource's row is painted. <c>null</c> paints every
    /// visible row.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResourceId { get; set; }
}
