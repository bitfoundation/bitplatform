using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The texts drawn around a <see cref="BitResourceTimelineAllocation"/> bar, one per position. A position left null
/// draws nothing.
/// <para>
/// The top and bottom texts sit on a line just above and just below the main bar (past the icons positioned above or
/// below it): the start ones aligned with the bar's start edge, the center ones centered on it, the end ones aligned
/// with its end edge. The <see cref="Start"/> and <see cref="End"/> texts sit on the bar's center line, just before its
/// start and just after its end (past any delay bar and the icons outside that end).
/// </para>
/// <para>
/// The three texts of a line are drawn only where they do not overlap: on a bar too narrow for all of them, the center
/// one is drawn, then the start one where it clears it, then the end one where it clears both. With
/// <see cref="BitResourceTimelineOptions.StickyBarLabels"/>, the center one slides between the start and end ones
/// where there is room for it.
/// </para>
/// <para>
/// The first text of <see cref="TopCenter"/>, <see cref="TopStart"/>, <see cref="TopEnd"/>, <see cref="Start"/>,
/// <see cref="End"/>, <see cref="BottomCenter"/>, <see cref="BottomStart"/> and <see cref="BottomEnd"/>, in that order,
/// is the bar's label in its default tooltip and its accessible name.
/// </para>
/// </summary>
public class BitResourceTimelineBarTexts
{
    /// <summary>
    /// The text drawn above the bar, aligned with its start edge.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineBarText? TopStart { get; set; }

    /// <summary>
    /// The text drawn above the bar, centered on it.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineBarText? TopCenter { get; set; }

    /// <summary>
    /// The text drawn above the bar, aligned with its end edge.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineBarText? TopEnd { get; set; }

    /// <summary>
    /// The text drawn on the bar's center line just before its start edge (past its start delay bar and the icons
    /// outside its start).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineBarText? Start { get; set; }

    /// <summary>
    /// The text drawn on the bar's center line just after its end edge (past its end delay bar and the icons outside
    /// its end). With <see cref="BitResourceTimelineOptions.CollapseMatchingEdgeLabels"/>, it may be drawn once for this
    /// bar and the next one when it says the same as that bar's <see cref="Start"/> text.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineBarText? End { get; set; }

    /// <summary>
    /// The text drawn under the bar, aligned with its start edge.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineBarText? BottomStart { get; set; }

    /// <summary>
    /// The text drawn under the bar, centered on it.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineBarText? BottomCenter { get; set; }

    /// <summary>
    /// The text drawn under the bar, aligned with its end edge.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineBarText? BottomEnd { get; set; }
}
