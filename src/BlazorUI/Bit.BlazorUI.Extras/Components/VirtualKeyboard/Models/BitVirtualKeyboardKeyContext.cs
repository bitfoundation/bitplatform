namespace Bit.BlazorUI;

/// <summary>
/// What a <see cref="BitVirtualKeyboard.KeyTemplate"/> renders a key from.
/// </summary>
public sealed class BitVirtualKeyboardKeyContext
{
    /// <summary>
    /// The key, as compiled: every value merged and resolved.
    /// </summary>
    public BitVirtualKeyboardKey Key { get; init; } = default!;

    /// <summary>
    /// The keycap label.
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// The small second label, if any.
    /// </summary>
    public string? Hint { get; init; }
}