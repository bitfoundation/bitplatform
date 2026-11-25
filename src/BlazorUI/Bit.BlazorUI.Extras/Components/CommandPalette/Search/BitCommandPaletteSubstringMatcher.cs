namespace Bit.BlazorUI;

/// <summary>Simple case-insensitive substring matcher. Faster, no typo tolerance.</summary>
public sealed class BitCommandPaletteSubstringMatcher : IBitCommandPaletteMatcher
{
    /// <summary>Shared default instance.</summary>
    public static BitCommandPaletteSubstringMatcher Default { get; } = new();

    /// <summary>Fold diacritics and keyboard variants as <see cref="BitCommandPaletteFuzzyMatcher.Normalize"/> does. Default true.</summary>
    public bool Normalize { get; init; } = true;

    /// <summary>Finds <paramref name="query"/> in <paramref name="text"/> ignoring case, scoring a match at the start higher.</summary>
    public BitCommandPaletteFieldMatch Match(string query, string text)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BitCommandPaletteFieldMatch.Empty;
        }

        var idx = BitCommandPaletteTextFold.Find(text, query, BitCommandPaletteTextMatchKind.Anywhere, Normalize);
        if (idx < 0)
        {
            return BitCommandPaletteFieldMatch.None;
        }

        var score = query.Length * BitCommandPaletteFuzzyMatcher.ScoreMatch + (idx == 0 ? BitCommandPaletteFuzzyMatcher.BonusBoundary * 2 : 0);
        return new BitCommandPaletteFieldMatch(true, score, Enumerable.Range(idx, query.Length).ToArray());
    }
}
