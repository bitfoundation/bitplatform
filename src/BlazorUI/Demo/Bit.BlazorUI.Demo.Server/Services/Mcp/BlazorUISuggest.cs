namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// The "did you mean" every tool answers a name it could not resolve with.
/// <para>
/// A plain substring test is not good enough for names that all begin with <c>Bit</c> and are built
/// from the same handful of words: it answers "BitDataGrd" with "BitTag", because the letters of
/// "tag" happen to fall across "Da<b>ta</b>" and "<b>G</b>rd". A suggestion that is worse than
/// silence trains an agent to stop reading them, so the miss is measured instead - by edit distance
/// over the names with their shared prefix removed, which is where the difference actually is.
/// </para>
/// </summary>
public static class BlazorUISuggest
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
            .Select(name => (Name: name, Distance: Distance(needle, Normalize(name))))
            // A name that CONTAINS what was typed is a hit however long the rest of it is
            // ("Dropdown" for "drop"), which no edit distance can express - so it is scored as an
            // exact match and ordered by how much of it is the typed part.
            .Select(hit => hit.Distance <= MaxDistance ? hit
                         : Normalize(hit.Name).Contains(needle, StringComparison.Ordinal) ? (hit.Name, Distance: 0)
                         : (hit.Name, Distance: int.MaxValue))
            .Where(hit => hit.Distance <= MaxDistance)
            .OrderBy(hit => hit.Distance)
            .ThenBy(hit => hit.Name.Length)
            .ThenBy(hit => hit.Name, StringComparer.Ordinal)
            .Select(hit => hit.Name)
            .Take(MaxSuggestions)];
    }

    /// <summary>
    /// A name reduced to what distinguishes it: lower-cased, non-alphanumerics dropped, and the
    /// <c>Bit</c> prefix every name here carries removed, since a prefix they all share cannot tell
    /// two of them apart and only dilutes the distance between them.
    /// </summary>
    private static string Normalize(string? name)
    {
        var text = new string([.. (name ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

        return text.StartsWith("bit", StringComparison.Ordinal) ? text[3..] : text;
    }

    /// <summary>
    /// Whether two words are within <paramref name="maxDistance"/> edits of each other - the same
    /// measure the suggestions are ranked by, offered to the callers that compare single words
    /// rather than whole names, so a typo is forgiven the same way everywhere.
    /// </summary>
    public static bool Within(string left, string right, int maxDistance)
        => maxDistance > 0 && Distance(left, right, maxDistance) <= maxDistance;

    private static int Distance(string left, string right) => Distance(left, right, MaxDistance);

    /// <summary>
    /// The Levenshtein distance between two names, abandoned as soon as it passes what a suggestion
    /// is worth: the catalogs hold a thousand names and a full matrix for each of them, on a call
    /// that only happens after a miss, is work spent on rows that cannot win.
    /// </summary>
    private static int Distance(string left, string right, int maxDistance)
    {
        if (Math.Abs(left.Length - right.Length) > maxDistance) return int.MaxValue;

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

            // Every distance from here on is at least the best of this row, so a row that is already
            // too far away settles it.
            if (best > maxDistance) return int.MaxValue;

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
