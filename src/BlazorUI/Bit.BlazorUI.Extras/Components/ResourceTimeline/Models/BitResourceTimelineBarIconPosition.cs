using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Where a <see cref="BitResourceTimelineBarIcon"/> is anchored relative to its owning
/// <see cref="BitResourceTimelineAllocation"/> bar.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitResourceTimelineBarIconPosition>))]
public enum BitResourceTimelineBarIconPosition
{
    /// <summary>Just before the bar's start edge, vertically centered on the bar.</summary>
    Start,

    /// <summary>Just after the bar's end edge, vertically centered on the bar.</summary>
    End,

    /// <summary>Centered above the bar.</summary>
    Above,

    /// <summary>Centered below the bar.</summary>
    Below,

    /// <summary>Drawn on top of the bar, centered horizontally and vertically within it.</summary>
    Center
}
