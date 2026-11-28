namespace Bit.BlazorUI;

/// <summary>Result of matching a query against one text field.</summary>
public readonly record struct BitCommandPaletteFieldMatch(bool Matched, double Score, int[] Positions)
{
    /// <summary>The text does not match.</summary>
    public static BitCommandPaletteFieldMatch None { get; } = new(false, double.NegativeInfinity, []);

    /// <summary>An empty query: everything matches with a score of 0.</summary>
    public static BitCommandPaletteFieldMatch Empty { get; } = new(true, 0, []);
}
