using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The modifier state that selects a layer of a <see cref="BitVirtualKeyboard"/> layout.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<BitVirtualKeyboardModifiers>))]
public enum BitVirtualKeyboardModifiers
{
    /// <summary>No modifier: the base layer, or a layer reached through a layer key.</summary>
    None = 0,

    /// <summary>The shift layer.</summary>
    Shift = 1,

    /// <summary>The AltGr layer (with Shift, the Shift+AltGr layer).</summary>
    AltGr = 2,

    /// <summary>The caps lock layer.</summary>
    Caps = 4
}