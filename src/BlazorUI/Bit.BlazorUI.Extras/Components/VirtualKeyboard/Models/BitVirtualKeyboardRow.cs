using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// A row of keys of a <see cref="BitVirtualKeyboardLayer"/>. A string converts to a row through the shorthand of
/// <see cref="BitVirtualKeyboardShorthand"/>.
/// </summary>
[JsonConverter(typeof(BitVirtualKeyboardRowJsonConverter))]
public sealed record BitVirtualKeyboardRow
{
    /// <summary>
    /// The keys of the row, left to right.
    /// </summary>
    public List<BitVirtualKeyboardKey> Keys { get; init; } = [];

    /// <summary>
    /// Where the row sits when it is narrower than the widest row and has no stretching key. Default: Center.
    /// </summary>
    public BitVirtualKeyboardRowAlign Align { get; init; } = BitVirtualKeyboardRowAlign.Center;



    /// <summary>
    /// Parses a row written in the shorthand of <see cref="BitVirtualKeyboardShorthand"/>.
    /// </summary>
    public static implicit operator BitVirtualKeyboardRow(string shorthand) => BitVirtualKeyboardShorthand.ParseRow(shorthand);
}