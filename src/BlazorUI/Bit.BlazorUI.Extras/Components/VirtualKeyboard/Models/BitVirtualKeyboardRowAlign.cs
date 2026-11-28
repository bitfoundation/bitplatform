using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// How a row of a <see cref="BitVirtualKeyboard"/> layout narrower than the widest row is placed, when it has no
/// stretching key to take the room left.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitVirtualKeyboardRowAlign>))]
public enum BitVirtualKeyboardRowAlign
{
    /// <summary>
    /// Centered, as the letter rows of a keyboard are.
    /// </summary>
    Center,

    /// <summary>
    /// At the start (the left; the key grid is never mirrored).
    /// </summary>
    Start,

    /// <summary>
    /// At the end (the right).
    /// </summary>
    End
}