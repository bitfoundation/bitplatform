using System;

namespace Bit.Butil;

// see https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent
/// <summary>
/// Contains properties of the event describing user interaction with the keyboard.
/// </summary>
public class ButilKeyboardEventArgs : EventArgs
{
    internal static readonly string[] EventArgsMembers = ["altKey", "code", "ctrlKey", "isComposing", "key", "location", "metaKey", "repeat", "shiftKey"];

    /// <summary>
    /// Returns a boolean value that is true if the Alt (Option or ⌥ on macOS) key was active when the key event was generated.
    /// </summary>
    public bool AltKey { get; set; }

    /// <summary>
    /// Returns a string with the code value of the physical key represented by the event.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Returns a boolean value that is true if the Ctrl key was active when the key event was generated.
    /// </summary>
    public bool CtrlKey { get; set; }

    /// <summary>
    /// Returns a boolean value that is true if the event is fired between after compositionstart and before compositionend.
    /// </summary>
    public bool IsComposing { get; set; }

    /// <summary>
    /// Returns a string representing the key value of the key represented by the event.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Returns a number representing the location of the key on the keyboard or other input device.
    /// </summary>
    public byte Location { get; set; }

    /// <summary>
    /// Returns a boolean value that is true if the Meta key (on Mac keyboards, the ⌘ Command key; on Windows keyboards, the Windows key (⊞)) was active when the key event was generated.
    /// </summary>
    public bool MetaKey { get; set; }

    /// <summary>
    /// Returns a boolean value that is true if the key is being held down such that it is automatically repeating.
    /// </summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// Returns a boolean value that is true if the Shift key was active when the key event was generated.
    /// </summary>
    public bool ShiftKey { get; set; }

}
