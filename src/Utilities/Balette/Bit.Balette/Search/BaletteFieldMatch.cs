namespace Bit.Balette;

/// <summary>Result of matching a query against one text field.</summary>
public readonly record struct BaletteFieldMatch(bool Matched, double Score, int[] Positions)
{
    /// <summary>The text does not match.</summary>
    public static BaletteFieldMatch None { get; } = new(false, double.NegativeInfinity, []);

    /// <summary>An empty query: everything matches with a score of 0.</summary>
    public static BaletteFieldMatch Empty { get; } = new(true, 0, []);
}
