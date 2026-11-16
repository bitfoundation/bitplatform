namespace Bit.Bink;

/// <summary>
/// A count with its noun, the way an announcement says it: "1 element", "3 elements". The canvas and every tool
/// announce through this, so a screen reader never hears "1 elements" from one of them and "1 element" from another.
/// </summary>
internal static class BinkCount
{
    /// <summary>
    /// <paramref name="count"/> followed by <paramref name="noun"/>, with an "s" for every count but one. Every noun an
    /// announcement counts (element, stroke, point, layer) takes a plain "s".
    /// </summary>
    public static string Of(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
