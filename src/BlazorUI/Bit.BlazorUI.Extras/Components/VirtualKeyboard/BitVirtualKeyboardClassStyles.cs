namespace Bit.BlazorUI;

public class BitVirtualKeyboardClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitVirtualKeyboard.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the panel of the keys (fixed to the bottom of the viewport in Docked mode) of the BitVirtualKeyboard.
    /// </summary>
    public string? Panel { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the wrapper of the layers of the BitVirtualKeyboard.
    /// </summary>
    public string? Layers { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each layer of keys of the BitVirtualKeyboard.
    /// </summary>
    public string? Layer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each key of the BitVirtualKeyboard.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each key that types a character (and the space bar) of the BitVirtualKeyboard.
    /// </summary>
    public string? CharacterKey { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each function key (Backspace, Tab, the layer keys, ...) of the BitVirtualKeyboard.
    /// </summary>
    public string? FunctionKey { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each modifier key (Shift, Caps Lock, AltGr) of the BitVirtualKeyboard.
    /// </summary>
    public string? ModifierKey { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for a key while it is active (a latched or locked modifier, a pending dead key, a held physical key); applied by the script of the BitVirtualKeyboard.
    /// </summary>
    public string? ActiveKey { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the Enter key of the BitVirtualKeyboard.
    /// </summary>
    public string? PrimaryKey { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each invisible spacer of the BitVirtualKeyboard.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each visible but inert key of the BitVirtualKeyboard.
    /// </summary>
    public string? Blank { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label of each key of the BitVirtualKeyboard.
    /// </summary>
    public string? KeyLabel { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the small second label of each key of the BitVirtualKeyboard.
    /// </summary>
    public string? KeyHint { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of each key of the BitVirtualKeyboard.
    /// </summary>
    public string? KeyIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the popup of long-press alternates of the BitVirtualKeyboard.
    /// </summary>
    public string? Popup { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each alternate of the long-press popup; applied by the script of the BitVirtualKeyboard.
    /// </summary>
    public string? PopupKey { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the enlarged-key bubble shown while a key is pressed of the BitVirtualKeyboard.
    /// </summary>
    public string? Preview { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden live region that announces the layer and modifier changes of the BitVirtualKeyboard.
    /// </summary>
    public string? LiveRegion { get; set; }
}
