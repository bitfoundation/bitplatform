namespace Bit.BlazorUI;

/// <summary>
/// The arguments of <see cref="BitVirtualKeyboard.OnKeyPress"/>, raised before a key acts.
/// </summary>
public sealed class BitVirtualKeyboardKeyPressArgs
{
    /// <summary>
    /// The id of the key: its bag id, its built-in name, or the text it types. Empty for a key that types a character in
    /// secure mode, whose id could tell it.
    /// </summary>
    public string KeyId { get; init; } = "";

    /// <summary>
    /// What the key does.
    /// </summary>
    public BitVirtualKeyboardKeyAction Action { get; init; }

    /// <summary>
    /// What the key will type; a handler may replace it. Null for a key that types nothing, and in secure mode.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The id of the layer the key is on.
    /// </summary>
    public string Layer { get; init; } = "";

    /// <summary>
    /// Set it to true to make the key do nothing.
    /// </summary>
    public bool Cancel { get; set; }
}