namespace Bit.BlazorUI;

/// <summary>
/// A layer of a <see cref="BitVirtualKeyboardLayout"/>: rows of keys shown together (the base letters, the shift
/// letters, the symbols, ...).
/// </summary>
public sealed record BitVirtualKeyboardLayer
{
    /// <summary>
    /// The id of the layer ("base", "shift", "caps", "altgr", "shift-altgr", "symbols", ...), which layer keys switch to.
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    /// The name of the layer, announced when it is switched to and shown on the keys that switch to it.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The modifier state that selects this layer (Shift, AltGr, Caps, or none for the base layer and for a layer
    /// reached through a layer key).
    /// </summary>
    public BitVirtualKeyboardModifiers Modifiers { get; init; }

    /// <summary>
    /// The rows of the layer, top to bottom. A row can be given as a shorthand string
    /// (<c>Rows = ["q w e r t y u i o p", "{shift*1.5} z x c {bksp*1.5}"]</c>).
    /// </summary>
    public List<BitVirtualKeyboardRow> Rows { get; init; } = [];
}