using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Colors the timeline content pane, under the grid and the bars. Assign to
/// <see cref="BitResourceTimelineOptions.Surface"/>. Unlike
/// <see cref="BitResourceTimelineColors"/>, a new instance replaces the
/// previous surface entirely, so an empty instance clears it. Visual only:
/// hit-testing, snap and scale are unchanged.
/// <para>
/// Painted bottom to top: the content background, then row fills, then column
/// stripes, then <see cref="Bands"/>, then the non-working wash
/// (<c>NonWorkingDays</c> / working hours) if that is on. A pattern is evaluated
/// for the whole time range and resource list, so a stripe stays on the same
/// day or row as the user scrolls. Stripes thinner than about a pixel, or more
/// than a few hundred in one frame, are left undrawn.
/// </para>
/// </summary>
public class BitResourceTimelineSurface
{
    /// <summary>
    /// Vertical time stripes. Two colors on the day unit alternate calendar days.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineColumnStripes? Columns { get; set; }

    /// <summary>Horizontal stripes down the resource list.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineRowStripes? Rows { get; set; }

    /// <summary>
    /// How a row color and a column color share a cell. <c>null</c> paints
    /// columns over rows.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineSurfaceCombine? Combine { get; set; }

    /// <summary>
    /// CSS color per resource id, for rows that do not set
    /// <see cref="BitResourceTimelineResource.Background"/>. Keys are resource
    /// ids. A row's own background wins over this map, and both win over
    /// <see cref="Rows"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? ResourceColors { get; set; }

    /// <summary>
    /// Rectangles painted above the patterns. Later entries paint over earlier ones.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<BitResourceTimelineSurfaceBand>? Bands { get; set; }

    /// <summary>
    /// Also paint column colors on the time axis: the date row for day and week
    /// stripes, the hour row for hour stripes. Labels stay above the tint.
    /// <c>null</c> and <c>false</c> leave the axis on its own background.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ShadeTimeAxis { get; set; }

    /// <summary>
    /// Also paint each row's color in the resource column, behind the label.
    /// <c>null</c> and <c>false</c> leave the column on its own background.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ShadeResourceAxis { get; set; }
}
