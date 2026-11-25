using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Where a <see cref="BitVirtualKeyboard"/> is shown.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitVirtualKeyboardMode>))]
public enum BitVirtualKeyboardMode
{
    /// <summary>
    /// Rendered in place, in the flow of the page, and always shown.
    /// </summary>
    Inline,

    /// <summary>
    /// Fixed to the bottom of the viewport, shown while it is <see cref="BitVirtualKeyboard.Open"/>.
    /// </summary>
    Docked
}