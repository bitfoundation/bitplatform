using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Where the text or icon of a <see cref="BitResourceTimelineBarSection"/> sits
/// within that section.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitResourceTimelineBarSectionPosition>))]
public enum BitResourceTimelineBarSectionPosition
{
    /// <summary>Just inside the section's start edge.</summary>
    Start,

    /// <summary>Centered horizontally within the section.</summary>
    Center,

    /// <summary>Just inside the section's end edge.</summary>
    End
}
