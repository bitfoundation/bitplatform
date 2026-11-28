using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The digits the purpose pads of a <see cref="BitVirtualKeyboard"/> type.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitVirtualKeyboardDigits>))]
public enum BitVirtualKeyboardDigits
{
    /// <summary>
    /// 0-9.
    /// </summary>
    Latin,

    /// <summary>
    /// The Arabic-Indic digits (U+0660-U+0669).
    /// </summary>
    ArabicIndic,

    /// <summary>
    /// The Persian (extended Arabic-Indic) digits (U+06F0-U+06F9).
    /// </summary>
    Persian
}