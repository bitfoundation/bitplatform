using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// A text drawn at one of the positions around a <see cref="BitResourceTimelineAllocation"/> bar (see
/// <see cref="BitResourceTimelineBarTexts"/>), with its own color, font and offset. A string converts to one implicitly,
/// so <c>Texts = new() { TopCenter = "AB123" }</c> draws a text in the default color and font.
/// </summary>
public class BitResourceTimelineBarText
{
    /// <summary>
    /// The text to draw. Nothing is drawn at the position while it is null or empty.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// CSS color the text is drawn in (for example <c>"#e8590c"</c> or <c>"tomato"</c>). Falls back to
    /// <see cref="BitResourceTimelineColors.BarLabel"/> when null or empty.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; set; }

    /// <summary>
    /// CSS font shorthand the text is drawn in (for example <c>"italic 600 12px sans-serif"</c>). Falls back to
    /// <see cref="BitResourceTimelineOptions.BarLabelFont"/> (or the bit theme's font) when null or empty. The text is
    /// measured in it too, so the room kept for it around the bar follows its size.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Font { get; set; }

    /// <summary>
    /// Horizontal distance in pixels the text is moved from where its position puts it; positive moves it to the
    /// right, negative to the left. Only the drawing moves: the room kept for the text around the bar (and so the
    /// stacking of the bars) is that of its position.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? OffsetX { get; set; }

    /// <summary>
    /// Vertical distance in pixels the text is moved from where its position puts it; positive moves it down,
    /// negative up. Only the drawing moves: the room kept for the text around the bar (and so the stacking of the
    /// bars) is that of its position.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? OffsetY { get; set; }



    /// <summary>
    /// A text drawn in the default color and font, without an offset; null for a null string.
    /// </summary>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(text))]
    public static implicit operator BitResourceTimelineBarText?(string? text) => text is null ? null : new() { Text = text };
}
