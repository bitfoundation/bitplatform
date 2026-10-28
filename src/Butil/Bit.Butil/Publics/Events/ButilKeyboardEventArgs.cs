using System;

namespace Bit.Butil;

// see https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent
/// <summary>
/// Contains properties of the event describing user interaction with the keyboard.
/// </summary>
public class ButilKeyboardEventArgs : EventArgs
{
    internal static readonly string[] EventArgsMembers = ["altKey", "code", "ctrlKey", "isComposing", "key", "location", "metaKey", "repeat", "shiftKey", "targetTagName", "targetIsEditable"];

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

    /// <summary>
    /// The local name, in upper case, of the element the key went to (<c>INPUT</c>, <c>TEXTAREA</c>, <c>BODY</c>, ...), or
    /// an empty string when the target is not an element. Upper case on any page and for any namespace - an XHTML
    /// document's <c>tagName</c> keeps the lower case and an SVG element's keeps its mixed case, so it is not read from
    /// there. Read through the event's composed path, so a key typed into an input inside an open shadow root reports
    /// that input rather than the shadow host; inside a closed one it reports the host, which is all the page can see.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/localName">Element.localName</see>
    /// </summary>
    public string TargetTagName { get; set; } = string.Empty;

    /// <summary>
    /// Returns true if the element the key went to acts on that key itself - the same test a <see cref="Keyboard"/>
    /// shortcut applies for <see cref="KeyboardShortcutOptions.IgnoreWhenEditable"/>: Delete in a text field, an arrow
    /// on a slider or in a radio group, Space on a checkbox. A shortcut handler checks it to stand aside for the key,
    /// so Delete on a <c>select</c> or an arrow on a checkbox still reads false.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/isContentEditable">HTMLElement.isContentEditable</see>
    /// </summary>
    public bool TargetIsEditable { get; set; }

}
