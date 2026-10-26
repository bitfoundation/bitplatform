using System.Globalization;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Ready-made text matchers for comboboxes and command palettes.
/// All matching is case- and accent-insensitive.
/// </summary>
public static class BitBhFilter
{
    private static readonly CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions Options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>True when <paramref name="text"/> contains <paramref name="query"/> (empty query matches everything).</summary>
    public static bool Contains(string? text, string? query)
        => string.IsNullOrWhiteSpace(query) || (text is not null && Compare.IndexOf(text, query.Trim(), Options) >= 0);

    /// <summary>True when <paramref name="text"/> starts with <paramref name="query"/>.</summary>
    public static bool StartsWith(string? text, string? query)
        => string.IsNullOrWhiteSpace(query) || (text is not null && Compare.IsPrefix(text, query.Trim(), Options));

    /// <summary>True when every whitespace-separated word of <paramref name="query"/> appears in <paramref name="text"/>.</summary>
    public static bool AllWords(string? text, string? query)
        => string.IsNullOrWhiteSpace(query)
            || query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).All(w => Contains(text, w));

    /// <summary>
    /// Fuzzy relevance score in [0, 1] (0 = no match): characters of <paramref name="query"/>
    /// must appear in order; consecutive runs, word starts and prefixes score higher.
    /// Inspired by the <c>command-score</c> algorithm used by cmdk.
    /// </summary>
    public static double Score(string? text, string? query, IEnumerable<string>? keywords = null)
    {
        if (string.IsNullOrWhiteSpace(query)) return 1;
        var best = ScoreOne(text, query.Trim());
        if (keywords is not null)
        {
            foreach (var keyword in keywords)
                best = Math.Max(best, ScoreOne(keyword, query.Trim()) * 0.9);
        }
        return best;
    }

    /// <summary>Filters and sorts <paramref name="items"/> by <see cref="Score"/> against <paramref name="query"/>.</summary>
    public static IEnumerable<T> Fuzzy<T>(IEnumerable<T> items, string? query, Func<T, string?> text)
        => string.IsNullOrWhiteSpace(query)
            ? items
            : items.Select(i => (item: i, score: Score(text(i), query)))
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .Select(x => x.item);

    private static double ScoreOne(string? text, string query)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var t = Normalize(text);
        var q = Normalize(query);
        if (t.Length == 0) return 0;
        if (t == q) return 1;
        if (t.StartsWith(q, StringComparison.Ordinal)) return 0.95 - 0.05 * (1 - (double)q.Length / t.Length);

        var contiguous = t.IndexOf(q, StringComparison.Ordinal);
        if (contiguous >= 0)
            return 0.8 - 0.2 * ((double)contiguous / t.Length) + (IsWordStart(t, contiguous) ? 0.05 : 0);

        // Subsequence match with bonuses.
        double score = 0;
        var ti = 0;
        var run = 0;
        foreach (var c in q)
        {
            var found = t.IndexOf(c, ti);
            if (found < 0) return 0;
            var bonus = found == ti ? 1.0 + run * 0.5 : 0.5;
            if (IsWordStart(t, found)) bonus += 0.8;
            run = found == ti ? run + 1 : 0;
            score += bonus;
            ti = found + 1;
        }
        var max = q.Length * 2.5;
        return Math.Clamp(0.6 * score / max, 0.01, 0.6);
    }

    private static bool IsWordStart(string text, int index)
        => index == 0 || !char.IsLetterOrDigit(text[index - 1]);

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).ToLowerInvariant();
    }
}
