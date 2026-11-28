using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// What a key of a <see cref="BitVirtualKeyboard"/> does.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitVirtualKeyboardKeyAction>))]
public enum BitVirtualKeyboardKeyAction
{
    /// <summary>Types the key's Output.</summary>
    Character,

    /// <summary>Deletes backwards.</summary>
    Backspace,

    /// <summary>Deletes forwards.</summary>
    Delete,

    /// <summary>The Enter key.</summary>
    Enter,

    /// <summary>Types a space.</summary>
    Space,

    /// <summary>Moves the focus to the next field.</summary>
    Tab,

    /// <summary>Moves the focus to the previous field.</summary>
    ShiftTab,

    /// <summary>Shift: one character from the shift layer; a double tap locks it.</summary>
    Shift,

    /// <summary>Caps Lock: toggles the caps layer.</summary>
    CapsLock,

    /// <summary>AltGr: one character from the AltGr layer.</summary>
    AltGr,

    /// <summary>Switches to the key's Layer, and stays there.</summary>
    SwitchLayer,

    /// <summary>Sets the key's dead-key marker; the next key is typed with it.</summary>
    DeadKey,

    /// <summary>Moves to the next of the keyboard's Languages.</summary>
    NextLayout,

    /// <summary>Closes a docked keyboard.</summary>
    Hide,

    /// <summary>Empties the value.</summary>
    Clear,

    /// <summary>Commits the value and closes a docked keyboard.</summary>
    Accept,

    /// <summary>Puts the value back to what it was and closes a docked keyboard.</summary>
    Cancel,

    /// <summary>Moves the caret left.</summary>
    ArrowLeft,

    /// <summary>Moves the caret right.</summary>
    ArrowRight,

    /// <summary>Moves the caret up a line.</summary>
    ArrowUp,

    /// <summary>Moves the caret down a line.</summary>
    ArrowDown,

    /// <summary>Moves the caret to the start of the line.</summary>
    Home,

    /// <summary>Moves the caret to the end of the line.</summary>
    End,

    /// <summary>Raises OnCommand with the key's Command.</summary>
    Custom
}