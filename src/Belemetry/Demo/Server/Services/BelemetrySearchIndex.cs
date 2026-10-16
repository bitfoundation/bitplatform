using Bit.Belemetry.Demo.Client.Shared;
using Bit.Belemetry.Demo.Server.Dtos;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// One search over everything the MCP server knows - the README's sections, the demo pages (by the words
/// <see cref="NavItem"/> describes them with), every public type of the reflected API, and the reference
/// tables - each hit carrying the exact tool call that returns its full text.
/// </summary>
/// <remarks>
/// A plain word-overlap score is enough at this size (a few hundred entries) and, unlike a fuzzy ranker, it
/// explains itself: a hit ranks where it does because its title or keywords contain the words asked for.
/// </remarks>
public static class BelemetrySearchIndex
{
    private sealed record Entry(string Kind, string Title, string Keywords, string Body, string Location, string FollowUp);

    private static readonly Lazy<Entry[]> _entries = new(Build);

    // Words that carry no meaning in a question about a telemetry library.
    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "to", "of", "in", "on", "for", "and", "or", "is", "are", "how", "do", "i", "my", "with",
        "what", "why", "does", "can", "it", "be", "use", "using", "belemetry", "bit", "blazor", "set", "get",
    };

    /// <summary>The best <paramref name="limit"/> hits for <paramref name="query"/>.</summary>
    public static BelemetrySearchHitDto[] Search(string? query, int limit = 10)
    {
        var words = Words(query);

        if (words.Length == 0) return [];

        return [.. _entries.Value
                   .Select(entry => (entry, score: Score(entry, words)))
                   .Where(x => x.score > 0)
                   .OrderByDescending(x => x.score)
                   .ThenBy(x => x.entry.Title.Length)
                   .Take(Math.Clamp(limit, 1, 50))
                   .Select(x => new BelemetrySearchHitDto(x.entry.Kind, x.entry.Title, Snippet(x.entry.Body, words), x.entry.Location, x.entry.FollowUp))];
    }

    private static string[] Words(string? text)
        => [.. (text ?? string.Empty)
               .Split([' ', ',', '.', '?', '!', ':', ';', '(', ')', '"', '\'', '/', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries)
               .Where(w => _stopWords.Contains(w) is false)
               .Select(w => w.ToLowerInvariant())
               .Distinct()];

    private static int Score(Entry entry, string[] words)
    {
        var score = 0;

        foreach (var word in words)
        {
            var hit = 0;
            if (entry.Title.Contains(word, StringComparison.OrdinalIgnoreCase)) hit += 6;
            if (entry.Keywords.Contains(word, StringComparison.OrdinalIgnoreCase)) hit += 3;
            if (entry.Body.Contains(word, StringComparison.OrdinalIgnoreCase)) hit += 1;
            score += hit;
        }

        // Every word matching somewhere beats many matches of one word.
        var matched = words.Count(w => entry.Title.Contains(w, StringComparison.OrdinalIgnoreCase)
                                    || entry.Keywords.Contains(w, StringComparison.OrdinalIgnoreCase)
                                    || entry.Body.Contains(w, StringComparison.OrdinalIgnoreCase));

        return matched == 0 ? 0 : score + (matched == words.Length ? 10 : 0);
    }

    private static string Snippet(string body, string[] words)
    {
        var text = body.Replace('\n', ' ').Replace("  ", " ", StringComparison.Ordinal).Trim();
        if (text.Length <= 220) return text;

        var at = words.Select(w => text.IndexOf(w, StringComparison.OrdinalIgnoreCase)).Where(i => i >= 0).DefaultIfEmpty(0).Min();
        var start = Math.Max(0, at - 60);
        var length = Math.Min(220, text.Length - start);

        return (start > 0 ? "..." : string.Empty) + text.Substring(start, length).Trim() + (start + length < text.Length ? "..." : string.Empty);
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>();

        foreach (var section in BelemetrySourceCatalog.Sections)
        {
            var title = section.Parent is null ? section.Heading : $"{section.Parent} > {section.Heading}";
            entries.Add(new("guide", title, section.Parent ?? string.Empty, section.Body, $"README.md#{section.Anchor}",
                            $"GetBelemetrySourceFile(path: \"README.md#{section.Anchor}\")"));
        }

        foreach (var page in NavItem.All)
        {
            entries.Add(new("page", page.Title, $"{page.Keywords} {page.Category}", page.Description, page.Url,
                            $"GetBelemetrySourceFile(path: \"{page.SourcePath}\")"));
        }

        foreach (var type in BelemetryApiCatalog.All)
        {
            var members = string.Join(' ', type.Members.Select(m => m.Name));
            entries.Add(new("api", type.Name, $"{type.Kind} {members}", $"{type.Summary} Members: {members}", type.Name,
                            $"GetBelemetryApiDetails(typeName: \"{type.Name}\")"));
        }

        foreach (var table in BelemetryConventions.Tables)
        {
            var rows = string.Join(" | ", table.Rows.Select(r => string.Join(' ', r)));
            entries.Add(new("convention", $"{table.Section} ({table.Source})", string.Join(' ', table.Headers), rows, table.Section,
                            $"LookupBelemetryConventions(query: \"{table.Section}\")"));
        }

        return [.. entries];
    }
}
