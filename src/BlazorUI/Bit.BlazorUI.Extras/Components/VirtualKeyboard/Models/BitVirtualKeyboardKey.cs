namespace Bit.BlazorUI;

/// <summary>
/// A key of a <see cref="BitVirtualKeyboardLayout"/>, in its key bag or in a row.
/// </summary>
/// <remarks>
/// A key in a row is merged with the bag entry and the built-in key it names: the built-in key first, then the bag
/// entry, then what the row itself gives, each value that is set winning over the one before. So every value that takes
/// part in the merge is nullable, an unset one inheriting; the defaults (a Character key one unit wide and one row high)
/// apply only after the merge.
/// </remarks>
public sealed record BitVirtualKeyboardKey
{
    /// <summary>
    /// The id the key is referenced by: a bag id, or the name of a built-in key ("bksp", "layer:symbols").
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// The text the key types. It can be more than one character (".com", "لا").
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// The keycap. Default: the layout's Displays entry of the key, else the text it types.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// A small second label in a corner of the key ("ABC" on the 2 of a phone pad).
    /// </summary>
    public string? Hint { get; init; }

    /// <summary>
    /// The icon of the key, in place of a text label.
    /// </summary>
    public BitIconInfo? Icon { get; init; }

    /// <summary>
    /// The name of the bit icon of the key, in place of a text label.
    /// </summary>
    public string? IconName { get; init; }

    /// <summary>
    /// The accessible name of the key. It has to contain the visible label.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    /// What the key does. Default: Character, which types its Output.
    /// </summary>
    public BitVirtualKeyboardKeyAction? Action { get; init; }

    /// <summary>
    /// The layer a SwitchLayer key switches to.
    /// </summary>
    public string? Layer { get; init; }

    /// <summary>
    /// The layer to go back to after the key has typed (a symbol key returning to the letters).
    /// </summary>
    public string? NextLayer { get; init; }

    /// <summary>
    /// The marker a DeadKey key sets, looked up in the layout's DeadKeys.
    /// </summary>
    public string? DeadKey { get; init; }

    /// <summary>
    /// The command a Custom key raises OnCommand with.
    /// </summary>
    public string? Command { get; init; }

    /// <summary>
    /// The width of the key, in key units (one letter key), in steps of 0.25. Default: 1.
    /// </summary>
    public double? Width { get; init; }

    /// <summary>
    /// The number of rows the key spans downwards. Default: 1.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// The empty room before the key, in key units. Default: 0.
    /// </summary>
    public double? Offset { get; init; }

    /// <summary>
    /// Whether the key takes the room its row leaves (the space bar).
    /// </summary>
    public bool? Stretch { get; init; }

    /// <summary>
    /// Whether the key is an invisible, inert spacer of its width.
    /// </summary>
    public bool? Gap { get; init; }

    /// <summary>
    /// Whether the key is a visible but inert key.
    /// </summary>
    public bool? Blank { get; init; }

    /// <summary>
    /// The alternates a long press of the key offers. Default: the layout's LongPress entry of the text it types.
    /// </summary>
    public List<string>? LongPress { get; init; }

    /// <summary>
    /// The alternate the long-press popup starts on.
    /// </summary>
    public string? LongPressDefault { get; init; }

    /// <summary>
    /// The texts repeated taps of the key cycle through.
    /// </summary>
    public List<string>? MultiTap { get; init; }

    /// <summary>
    /// Whether the key repeats while held. Default: Backspace, Delete, Space and the arrows repeat.
    /// </summary>
    public bool? Repeat { get; init; }

    /// <summary>
    /// Whether a press of the key shows the enlarged-key bubble. Default: the character keys do.
    /// </summary>
    public bool? Preview { get; init; }

    /// <summary>
    /// The purposes the key is there for; it is left out for any other (the .com key of the email and URL layouts).
    /// </summary>
    public List<BitVirtualKeyboardPurpose>? Purposes { get; init; }

    /// <summary>
    /// What changes on the key for a purpose (the comma key typing @ for an email address).
    /// </summary>
    public Dictionary<BitVirtualKeyboardPurpose, BitVirtualKeyboardKey>? Variations { get; init; }

    /// <summary>
    /// Whether the key takes part in scrambling. Default: the keys typing a single digit do.
    /// </summary>
    public bool? Scramble { get; init; }

    /// <summary>
    /// Custom CSS classes of the key.
    /// </summary>
    public string? Class { get; init; }

    /// <summary>
    /// Custom CSS styles of the key.
    /// </summary>
    public string? Style { get; init; }
}