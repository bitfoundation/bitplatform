namespace Bit.BlazorUI;

/// <summary>
/// The words a <see cref="BitVirtualKeyboard"/> says: the names of its keys and what it announces, for a page in
/// another language to translate. The keycaps of the layouts are data of the layouts themselves.
/// </summary>
public class BitVirtualKeyboardStrings
{
    /// <summary>
    /// What the keyboard is announced as, after its name. Default: "virtual keyboard".
    /// </summary>
    public string RoleDescription { get; set; } = "virtual keyboard";

    /// <summary>
    /// The name of the Backspace key. Default: "Backspace".
    /// </summary>
    public string Backspace { get; set; } = "Backspace";

    /// <summary>
    /// The name of the Delete key. Default: "Delete".
    /// </summary>
    public string Delete { get; set; } = "Delete";

    /// <summary>
    /// The name of the Enter key. Default: "Enter".
    /// </summary>
    public string Enter { get; set; } = "Enter";

    /// <summary>
    /// The name of the Enter key of an input whose enterkeyhint is done. Default: "Done".
    /// </summary>
    public string Done { get; set; } = "Done";

    /// <summary>
    /// The name of the Enter key of an input whose enterkeyhint is go. Default: "Go".
    /// </summary>
    public string Go { get; set; } = "Go";

    /// <summary>
    /// The name of the Enter key of an input whose enterkeyhint is next. Default: "Next".
    /// </summary>
    public string Next { get; set; } = "Next";

    /// <summary>
    /// The name of the Enter key of an input whose enterkeyhint is previous. Default: "Previous".
    /// </summary>
    public string Previous { get; set; } = "Previous";

    /// <summary>
    /// The name of the Enter key of a search, or of an input whose enterkeyhint is search. Default: "Search".
    /// </summary>
    public string Search { get; set; } = "Search";

    /// <summary>
    /// The name of the Enter key of an input whose enterkeyhint is send. Default: "Send".
    /// </summary>
    public string Send { get; set; } = "Send";

    /// <summary>
    /// The name of the space bar. Default: "Space".
    /// </summary>
    public string Space { get; set; } = "Space";

    /// <summary>
    /// The name of the Tab key. Default: "Tab".
    /// </summary>
    public string Tab { get; set; } = "Tab";

    /// <summary>
    /// The name of the Shift+Tab key. Default: "Shift Tab".
    /// </summary>
    public string ShiftTab { get; set; } = "Shift Tab";

    /// <summary>
    /// The name of the Shift key. Default: "Shift".
    /// </summary>
    public string Shift { get; set; } = "Shift";

    /// <summary>
    /// The name of the Caps Lock key. Default: "Caps lock".
    /// </summary>
    public string CapsLock { get; set; } = "Caps lock";

    /// <summary>
    /// The name of the AltGr key. Default: "AltGr".
    /// </summary>
    public string AltGr { get; set; } = "AltGr";

    /// <summary>
    /// The name of the symbols layer, and of the key that switches to it. Default: "Symbols".
    /// </summary>
    public string Symbols { get; set; } = "Symbols";

    /// <summary>
    /// The name of the second symbols layer. Default: "More symbols".
    /// </summary>
    public string MoreSymbols { get; set; } = "More symbols";

    /// <summary>
    /// The name of the base layer, and of the key that switches back to it. Default: "Letters".
    /// </summary>
    public string Letters { get; set; } = "Letters";

    /// <summary>
    /// The name of the key that moves to the next language. Default: "Next language".
    /// </summary>
    public string NextLanguage { get; set; } = "Next language";

    /// <summary>
    /// The name of the key that closes a docked keyboard. Default: "Hide keyboard".
    /// </summary>
    public string Hide { get; set; } = "Hide keyboard";

    /// <summary>
    /// The name and label of the key that empties the value. Default: "Clear".
    /// </summary>
    public string Clear { get; set; } = "Clear";

    /// <summary>
    /// The name and label of the key that commits the value. Default: "Accept".
    /// </summary>
    public string Accept { get; set; } = "Accept";

    /// <summary>
    /// The name and label of the key that puts the value back. Default: "Cancel".
    /// </summary>
    public string Cancel { get; set; } = "Cancel";

    /// <summary>
    /// The name of the left arrow key. Default: "Left".
    /// </summary>
    public string ArrowLeft { get; set; } = "Left";

    /// <summary>
    /// The name of the right arrow key. Default: "Right".
    /// </summary>
    public string ArrowRight { get; set; } = "Right";

    /// <summary>
    /// The name of the up arrow key. Default: "Up".
    /// </summary>
    public string ArrowUp { get; set; } = "Up";

    /// <summary>
    /// The name of the down arrow key. Default: "Down".
    /// </summary>
    public string ArrowDown { get; set; } = "Down";

    /// <summary>
    /// The name and label of the Home key. Default: "Home".
    /// </summary>
    public string Home { get; set; } = "Home";

    /// <summary>
    /// The name and label of the End key. Default: "End".
    /// </summary>
    public string End { get; set; } = "End";

    /// <summary>
    /// The name and label of the quantity key of the POS pad. Default: "Qty".
    /// </summary>
    public string Qty { get; set; } = "Qty";

    /// <summary>
    /// The name and label of the void key of the POS pad. Default: "Void".
    /// </summary>
    public string Void { get; set; } = "Void";

    /// <summary>
    /// The name and label of the total key of the POS pad. Default: "Total".
    /// </summary>
    public string Total { get; set; } = "Total";

    /// <summary>
    /// The name of the zero-width non-joiner key. Default: "zero-width non-joiner".
    /// </summary>
    public string Zwnj { get; set; } = "zero-width non-joiner";

    /// <summary>
    /// The name of the zero-width joiner key. Default: "zero-width joiner".
    /// </summary>
    public string Zwj { get; set; } = "zero-width joiner";

    /// <summary>
    /// The name of the left-to-right mark key. Default: "left-to-right mark".
    /// </summary>
    public string Lrm { get; set; } = "left-to-right mark";

    /// <summary>
    /// The name of the right-to-left mark key. Default: "right-to-left mark".
    /// </summary>
    public string Rlm { get; set; } = "right-to-left mark";

    /// <summary>
    /// The name of the no-break space key. Default: "no-break space".
    /// </summary>
    public string Nbsp { get; set; } = "no-break space";

    /// <summary>
    /// What is announced when Shift is latched. Default: "Shift on".
    /// </summary>
    public string ShiftOn { get; set; } = "Shift on";

    /// <summary>
    /// What is announced when Shift is locked. Default: "Shift locked".
    /// </summary>
    public string ShiftLocked { get; set; } = "Shift locked";

    /// <summary>
    /// What is announced when Shift is released. Default: "Shift off".
    /// </summary>
    public string ShiftOff { get; set; } = "Shift off";

    /// <summary>
    /// What is announced when Caps Lock is turned on. Default: "Caps lock on".
    /// </summary>
    public string CapsLockOn { get; set; } = "Caps lock on";

    /// <summary>
    /// What is announced when Caps Lock is turned off. Default: "Caps lock off".
    /// </summary>
    public string CapsLockOff { get; set; } = "Caps lock off";

    /// <summary>
    /// What is announced when the layer changes; {0} is the name of the layer. Default: "{0} keyboard".
    /// </summary>
    public string LayerAnnouncement { get; set; } = "{0} keyboard";

    /// <summary>
    /// The name of the popup of long-press alternates; {0} is the key it is opened from. Default: "Alternates for {0}".
    /// </summary>
    public string Alternates { get; set; } = "Alternates for {0}";
}
