using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// What the Enter key of a <see cref="BitVirtualKeyboard"/> does.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitVirtualKeyboardEnterBehavior>))]
public enum BitVirtualKeyboardEnterBehavior
{
    /// <summary>
    /// A new line in a textarea; the next or previous field for an enterkeyhint of next or previous; submits the form
    /// of any other input; nothing else.
    /// </summary>
    Auto,

    /// <summary>
    /// Always types a new line.
    /// </summary>
    NewLine,

    /// <summary>
    /// Always submits the form of the input.
    /// </summary>
    Submit,

    /// <summary>
    /// Always moves the focus to the next field.
    /// </summary>
    Next,

    /// <summary>
    /// Closes a docked keyboard.
    /// </summary>
    Close,

    /// <summary>
    /// Does nothing but raise OnEnter.
    /// </summary>
    None
}