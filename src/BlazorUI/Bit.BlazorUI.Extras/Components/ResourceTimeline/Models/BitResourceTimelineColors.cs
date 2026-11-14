using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Optional color overrides for the timeline. Every value is a CSS color string
/// (for example <c>"#ffffff"</c>, <c>"white"</c> or <c>"rgba(0,0,0,.1)"</c>).
/// Any property left <c>null</c> keeps the color already in force - the renderer's
/// or the bit theme's default on the first apply, and the one an earlier options
/// instance set on a later one - so a partial instance only changes the colors it
/// sets. Useful for theming (e.g. dark mode).
/// </summary>
public class BitResourceTimelineColors
{
    /// <summary>Background of the scrollable content area.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentBg { get; set; }

    /// <summary>Background of the time and resource axes (and their corner).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AxisBg { get; set; }

    /// <summary>Border/divider lines within and around the axes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AxisBorder { get; set; }

    /// <summary>Hour tick marks on the time axis.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tick { get; set; }

    /// <summary>Hour-of-day labels on the time axis.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }

    /// <summary>
    /// Labels of the date row, the top row of the time axis: a label per day, or per run of days, week,
    /// month, quarter, half year or year as the zoom drops.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DateLabel { get; set; }

    /// <summary>Grid lines drawn across the content area.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Grid { get; set; }

    /// <summary>Default bar fill, used when an allocation has no explicit color.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Bar { get; set; }

    /// <summary>Fill of a selected bar when it has no explicit color.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BarSelected { get; set; }

    /// <summary>Outline drawn around a selected bar.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BarSelectedBorder { get; set; }

    /// <summary>Color of bar labels (text above/below/start/end).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BarLabel { get; set; }

    /// <summary>The vertical "current time" indicator line and marker.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Now { get; set; }

    /// <summary>Fill of the rubber-band (marquee) selection rectangle.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SelectionFill { get; set; }

    /// <summary>Border of the rubber-band (marquee) selection rectangle.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SelectionBorder { get; set; }

    /// <summary>Dashed keyboard-focus ring drawn around the focused bar.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Focus { get; set; }

    /// <summary>Background of the hover tooltip.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TooltipBg { get; set; }

    /// <summary>Text color of the hover tooltip.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TooltipText { get; set; }

    /// <summary>
    /// Fill for non-working-time shading (weekends and off-hour bands). Defaults
    /// to a light gray wash.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NonWorking { get; set; }
}
