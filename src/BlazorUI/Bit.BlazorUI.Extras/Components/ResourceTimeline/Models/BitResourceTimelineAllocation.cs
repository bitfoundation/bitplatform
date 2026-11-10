using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// A single allocation period drawn as a bar on a resource row.
/// </summary>
public class BitResourceTimelineAllocation
{
    /// <summary>Unique identifier for the allocation bar.</summary>
    public required string Id { get; set; }

    /// <summary>Identifier of the owning <see cref="BitResourceTimelineResource"/>.</summary>
    public required string ResourceId { get; set; }

    /// <summary>Start of the period.</summary>
    [JsonConverter(typeof(BitResourceTimelineUnixMillisecondsConverter))]
    public DateTimeOffset StartTime { get; set; }

    /// <summary>End of the period.</summary>
    [JsonConverter(typeof(BitResourceTimelineUnixMillisecondsConverter))]
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Optional CSS color for the bar fill (for example <c>"#e8590c"</c> or <c>"tomato"</c>).
    /// Falls back to the renderer's default bar color when null or empty.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Optional height of the bar in pixels. The bar is vertically centered within
    /// its resource row regardless of height. Falls back to the renderer's default
    /// bar height when null or not greater than zero. Any start/end edge bars share
    /// this height.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>Optional label rendered centered above the bar.</summary>
    public string? TextAbove { get; set; }

    /// <summary>Optional label rendered centered below the bar.</summary>
    public string? TextBelow { get; set; }

    /// <summary>Optional label rendered just before the bar's start edge.</summary>
    public string? TextStart { get; set; }

    /// <summary>Optional label rendered just after the bar's end edge.</summary>
    public string? TextEnd { get; set; }

    /// <summary>
    /// Optional tooltip text shown when hovering the bar (requires
    /// <see cref="BitResourceTimelineOptions.ShowTooltips"/>). When null, a
    /// default tooltip is built from the bar's labels, resource name and time
    /// range. Use <c>"\n"</c> to separate lines.
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// Optional decorative bar drawn immediately before the main bar's start edge.
    /// Shares the main bar's row and height. Commonly used to indicate a delay
    /// before the planned allocation began.
    /// </summary>
    public BitResourceTimelineEdgeBar? StartBar { get; set; }

    /// <summary>
    /// Optional decorative bar drawn immediately after the main bar's end edge.
    /// Shares the main bar's row and height. Commonly used to indicate a delay
    /// after the planned allocation ended.
    /// </summary>
    public BitResourceTimelineEdgeBar? EndBar { get; set; }

    /// <summary>
    /// Optional custom icons or small images rendered around the bar. Each icon
    /// can be anchored to a different position (start, end, above, below or
    /// center) and drawn either beside the bar or within it. Multiple icons
    /// sharing a position are laid out next to one another, growing away from
    /// the bar in the order they appear in the list.
    /// </summary>
    public List<BitResourceTimelineBarIcon>? Icons { get; set; }

    /// <summary>
    /// Optional parts of the bar drawn with their own background, border and text
    /// colors, and their own text and icon. Each is bounded by times or by
    /// percentages of the bar, always within the bar, and drawn over it in list
    /// order. See <see cref="BitResourceTimelineBarSection"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<BitResourceTimelineBarSection>? Sections { get; set; }

    /// <summary>
    /// Optional host payload preserved on this instance. Serialized as camelCase
    /// <c>data</c> on the JS wire; the engine never reads it and does not paint it.
    /// Stuff a host DTO with <c>JsonSerializer.SerializeToElement</c>.
    /// </summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// When <c>true</c>, the bar can be selected and shows tooltips but cannot be
    /// moved or resized (pointer or keyboard). Defaults to <c>false</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Locked { get; set; }

    /// <summary>
    /// Optional CSS class applied to the bar element by the HTML renderer (in
    /// addition to <c>data-bar-id</c>). Canvas and SVG ignore it. Pointer events
    /// still go to the surface, so <c>:hover</c> on the bar does not fire; use
    /// the class for paint (stripes, hatch) via host CSS on
    /// <c>.bit-rtm-srf [data-bar-id]</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClassName { get; set; }
}
