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

    /// <summary>
    /// Centered horizontally within the section, or moved in just far enough to
    /// stay clear of what sits at the section's edges when centering would overlap it.
    /// </summary>
    Center,

    /// <summary>Just inside the section's end edge.</summary>
    End
}
