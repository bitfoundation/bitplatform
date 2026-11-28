namespace Bit.BlazorUI;

/// <summary>
/// The arguments of <see cref="BitVirtualKeyboard.OnCommand"/>, raised by a key whose action is Custom.
/// </summary>
public sealed class BitVirtualKeyboardCommandArgs
{
    /// <summary>
    /// The command of the key.
    /// </summary>
    public string Command { get; init; } = "";

    /// <summary>
    /// The id of the key.
    /// </summary>
    public string KeyId { get; init; } = "";
}