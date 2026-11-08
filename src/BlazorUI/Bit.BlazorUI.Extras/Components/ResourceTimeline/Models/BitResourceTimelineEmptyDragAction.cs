using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// What a pointer drag on empty content (no bar hit) does while editing.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitResourceTimelineEmptyDragAction>))]
public enum BitResourceTimelineEmptyDragAction
{
    /// <summary>Rubber-band selection (the default; no breaking change).</summary>
    Marquee,

    /// <summary>
    /// Draw a new allocation. Requires <see cref="BitResourceTimelineOptions.Editable"/>
    /// and an <c>OnAllocationCreating</c> handler. Ctrl/Cmd-drag still marquees.
    /// </summary>
    Create
}
