namespace Bit.Balette;

/// <summary>Simple case-insensitive substring matcher. Faster, no typo tolerance.</summary>
public sealed class BaletteSubstringMatcher : IBaletteMatcher
{
    /// <summary>Shared default instance.</summary>
    public static BaletteSubstringMatcher Default { get; } = new();

    /// <summary>Fold diacritics and keyboard variants as <see cref="BaletteFuzzyMatcher.Normalize"/> does. Default true.</summary>
    public bool Normalize { get; init; } = true;

    /// <summary>Finds <paramref name="query"/> in <paramref name="text"/> ignoring case, scoring a match at the start higher.</summary>
    public BaletteFieldMatch Match(string query, string text)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BaletteFieldMatch.Empty;
        }

        var idx = BaletteTextFold.Find(text, query, BaletteTextMatchKind.Anywhere, Normalize);
        if (idx < 0)
        {
            return BaletteFieldMatch.None;
        }

        var score = query.Length * BaletteFuzzyMatcher.ScoreMatch + (idx == 0 ? BaletteFuzzyMatcher.BonusBoundary * 2 : 0);
        return new BaletteFieldMatch(true, score, Enumerable.Range(idx, query.Length).ToArray());
    }
}
