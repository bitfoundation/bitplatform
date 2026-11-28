using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The form of a language layout of a <see cref="BitVirtualKeyboard"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitVirtualKeyboardForm>))]
public enum BitVirtualKeyboardForm
{
    /// <summary>
    /// Compact under the compact breakpoint of the keyboard's width, Full above it.
    /// </summary>
    Auto,

    /// <summary>
    /// The hardware-like form: the number row, every symbol on its own key, the AltGr layer.
    /// </summary>
    Full,

    /// <summary>
    /// The phone-like form: the letters, with the digits and symbols on layers of their own.
    /// </summary>
    Compact
}