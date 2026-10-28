namespace Bit.BlazorHeadlessUI.Demo.Server.Services;

/// <summary>
/// The "did you mean" every tool answers a name it could not resolve with.
/// <para>
/// A plain substring test is not good enough for names that all begin with <c>BitBh</c> and are built
/// from the same handful of words, so the miss is measured instead - by edit distance over the names
/// with their shared prefix removed, which is where the difference actually is.
/// </para>
/// </summary>
public static class BlazorHeadlessUISuggest
{
    /// <summary>How many edits a name may be from the one that was typed and still be worth offering.</summary>
    private const int MaxDistance = 3;

    private const int MaxSuggestions = 8;

    /// <summary>The names closest to one that resolved to nothing, best first.</summary>
    public static string[] Closest(string? typed, IEnumerable<string> names)
    {
        var needle = Normalize(typed);

        if (needle.Length == 0) return [];

        return [.. names
            .Distinct(StringComparer.Ordinal)
            .Select(name => (Name: name, Normalized: Normalize(name)))
            .Select(entry => (entry.Name, Distance: Distance(needle, entry.Normalized) is var distance && distance <= MaxDistance
                // A name that CONTAINS what was typed is a hit however long the rest of it is ("MenuItem"
                // for "item"), which no edit distance can express - so it ranks right after the typos.
                ? distance
                : entry.Normalized.Contains(needle, StringComparison.Ordinal) || (entry.Normalized.Length >= 4 && needle.Contains(entry.Normalized, StringComparison.Ordinal))
                    ? MaxDistance
                    : int.MaxValue))
            .Where(hit => hit.Distance <= MaxDistance)
            .OrderBy(hit => hit.Distance)
            .ThenBy(hit => hit.Name.Length)
            .ThenBy(hit => hit.Name, StringComparer.Ordinal)
            .Select(hit => hit.Name)
            .Take(MaxSuggestions)];
    }

    /// <summary>
    /// A name reduced to what distinguishes it: lower-cased, generic arguments and non-alphanumerics
    /// dropped, and the <c>BitBh</c> / <c>IBitBh</c> prefix every type here carries removed.
    /// </summary>
    public static string Normalize(string? name)
    {
        var text = name ?? string.Empty;

        var generic = text.IndexOf('<', StringComparison.Ordinal);
        if (generic > 0) text = text[..generic];

        text = new string([.. text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

        return text.StartsWith("ibitbh", StringComparison.Ordinal) ? text[6..]
             : text.StartsWith("bitbh", StringComparison.Ordinal) ? text[5..]
             : text;
    }

    /// <summary>
    /// The Levenshtein distance between two names, abandoned as soon as it passes what a suggestion
    /// is worth.
    /// </summary>
    private static int Distance(string left, string right)
    {
        if (Math.Abs(left.Length - right.Length) > MaxDistance) return int.MaxValue;

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++) previous[j] = j;

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            var best = current[0];

            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;

                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                best = Math.Min(best, current[j]);
            }

            // Every distance from here on is at least the best of this row.
            if (best > MaxDistance) return int.MaxValue;

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
