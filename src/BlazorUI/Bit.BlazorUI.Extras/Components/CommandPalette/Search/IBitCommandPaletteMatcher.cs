namespace Bit.BlazorUI;

/// <summary>Scores how well a query matches a text. Implement to plug in a custom algorithm.</summary>
public interface IBitCommandPaletteMatcher
{
    /// <summary>
    /// Match <paramref name="query"/> against <paramref name="text"/>.
    /// Returns <see cref="BitCommandPaletteFieldMatch.None"/> when the text does not match.
    /// Positions are indexes into <paramref name="text"/> to highlight.
    /// </summary>
    BitCommandPaletteFieldMatch Match(string query, string text);
}
