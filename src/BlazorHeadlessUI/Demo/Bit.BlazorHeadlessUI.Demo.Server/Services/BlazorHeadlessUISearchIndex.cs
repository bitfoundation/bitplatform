using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorHeadlessUI.Demo.Server.Services;

/// <summary>
/// One index over everything the other tools answer from - the families, their parts and every
/// parameter, the library's other types, every demo example and every section of the guide - so a
/// request in its own words ("close on escape", "select many", "aria-activedescendant") finds what
/// holds the answer, with the exact call that returns it.
/// </summary>
public static partial class BlazorHeadlessUISearchIndex
{
    private sealed record Entry(string Kind, string Title, string Line, string Call, string Name, string[] NameWords, string[] TextWords);

    private static readonly Lazy<Entry[]> _entries = new(Build, LazyThreadSafetyMode.PublicationOnly);

    // Words every query has and no entry is told apart by.
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "an", "and", "are", "as", "be", "by", "can", "do", "does", "for", "from", "how", "i", "in", "is", "it",
        "me", "my", "of", "on", "or", "that", "the", "this", "to", "use", "using", "what", "when", "which", "with", "you",
        "bit", "bh", "bitbh", "blazor", "component", "components",
    };

    // On a tie the kind that answers more of a question comes first.
    private static readonly string[] KindOrder = ["family", "part", "parameter", "type", "example", "guide"];

    /// <summary>Builds the index ahead of the first query, which would otherwise wait for it.</summary>
    public static void Warm() => _ = _entries.Value;

    public static string Search(string? query, int limit)
    {
        limit = Math.Clamp(limit, 1, 50);

        var words = Words(query ?? string.Empty).Where(word => StopWords.Contains(word) is false).Distinct().ToArray();
        var phrase = Normalize(query ?? string.Empty);

        if (words.Length == 0 && phrase.Length == 0)
        {
            return "Pass what the UI has to do, or a name, e.g. 'dropdown menu with submenus', 'close on escape', 'data-highlighted', 'BitBhListbox'.";
        }

        var hits = _entries.Value
            .Select(entry => (Entry: entry, Score: Score(entry, words, phrase)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => Array.IndexOf(KindOrder, hit.Entry.Kind))
            .ThenBy(hit => hit.Entry.Title.Length)
            .Take(limit)
            .ToArray();

        if (hits.Length == 0)
        {
            var similar = BlazorHeadlessUIComponentCatalog.Similar(query);

            return similar.Length > 0
                ? $"Nothing matched '{query}'. The closest names are: {string.Join(", ", similar)}."
                : $"Nothing matched '{query}'. `GetBitBlazorHeadlessUIComponent` with no argument lists every component family.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Best matches for '{query}':").AppendLine();

        var rank = 1;
        foreach (var (entry, _) in hits)
        {
            builder.AppendLine($"{rank++}. **{entry.Title}** ({entry.Kind}) - {entry.Line} -> `{entry.Call}`");
        }

        return builder.ToString().TrimEnd();
    }

    private static double Score(Entry entry, string[] words, string phrase)
    {
        double score = 0;
        var matched = 0;

        foreach (var word in words)
        {
            double best = 0;

            if (entry.Name == word) best = 10;
            else if (entry.NameWords.Contains(word)) best = 6;
            else if (word.Length >= 4 && entry.Name.Contains(word, StringComparison.Ordinal)) best = 4;
            else if (entry.TextWords.Contains(word)) best = 1.5;
            else if (word.Length >= 5 && entry.TextWords.Any(text => text.StartsWith(word[..^1], StringComparison.Ordinal))) best = 1;

            if (best > 0) matched++;
            score += best;
        }

        // The whole query spelled as one name ("menuitem", "BitBhMenuItem", "data-highlighted").
        if (phrase.Length >= 3 && entry.Name == phrase) score += 12;

        // A hit on every word beats a strong hit on one of them.
        if (words.Length > 1 && matched == words.Length) score *= 1.5;

        return score;
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>();

        foreach (var family in BlazorHeadlessUIComponentCatalog.Families)
        {
            var familyCall = $"GetBitBlazorHeadlessUIComponent(names: \"{family.Key}\")";

            entries.Add(new Entry("family", family.Label, $"{family.Area}. {family.Lead}", familyCall,
                                  Normalize(family.Label), [.. Words(family.Label), .. Words(family.Key)],
                                  [.. Words($"{family.Area} {family.Lead}"), .. family.Parts.SelectMany(part => SplitName(part.TagName))]));

            foreach (var part in family.Parts)
            {
                var partCall = $"GetBitBlazorHeadlessUIComponent(names: \"{part.TagName}\")";

                entries.Add(new Entry("part", part.Name,
                                      $"{family.Label}. {BlazorHeadlessUIComponentCatalog.FirstSentence(part.Summary)}",
                                      partCall,
                                      BlazorHeadlessUISuggest.Normalize(part.TagName), SplitName(part.TagName),
                                      [.. Words($"{part.Summary} {part.Remarks} {string.Join(' ', part.Hooks)}"), .. Hooks(part.Hooks)]));

                foreach (var parameter in part.Parameters)
                {
                    entries.Add(new Entry("parameter", $"{part.TagName}.{parameter.Name}",
                                          $"`{parameter.TypeName}`. {BlazorHeadlessUIComponentCatalog.FirstSentence(parameter.Summary)}",
                                          partCall,
                                          Normalize(parameter.Name), [.. SplitName(parameter.Name), .. SplitName(part.TagName)],
                                          Words(parameter.Summary ?? string.Empty)));
                }
            }

            // The CSS page's examples only: the Tailwind page shows the same ones, and the hit's call
            // takes styling: "tailwind" for that copy.
            foreach (var example in family.CssPage?.Examples ?? [])
            {
                entries.Add(new Entry("example", $"{family.Label}: {example.Title}",
                                      BlazorHeadlessUIComponentCatalog.FirstSentence(example.Description) ?? family.CssPage!.Route,
                                      $"GetBitBlazorHeadlessUIExamples(name: \"{family.Key}\", example: \"{example.Title}\")",
                                      Normalize(example.Title), Words(example.Title),
                                      Words($"{example.Description} {family.Label}")));
            }
        }

        foreach (var type in BlazorHeadlessUIComponentCatalog.Types)
        {
            var summary = BlazorHeadlessUIXmlDocs.GetSummary(BlazorHeadlessUIXmlDocs.IdOf(type));

            entries.Add(new Entry("type", BlazorHeadlessUITypeNames.Of(type),
                                  BlazorHeadlessUIComponentCatalog.FirstSentence(summary) ?? (type.IsEnum ? "enum" : "type"),
                                  $"GetBitBlazorHeadlessUIComponent(names: \"{BlazorHeadlessUIComponentCatalog.BareName(type)}\")",
                                  BlazorHeadlessUISuggest.Normalize(type.Name), SplitName(BlazorHeadlessUIComponentCatalog.BareName(type)),
                                  [.. Words(summary ?? string.Empty), .. type.IsEnum ? type.GetEnumNames().SelectMany(SplitName) : []]));
        }

        foreach (var section in BlazorHeadlessUIGuide.Sections)
        {
            entries.Add(new Entry("guide", section.Heading, $"a section of the guide ({section.Markdown.Length} characters)",
                                  $"GetBitBlazorHeadlessUIGuide(topic: \"{section.Heading}\")",
                                  Normalize(section.Heading), Words(section.Heading), Words(section.Markdown)));
        }

        return [.. entries];
    }

    /// <summary>A hook as one word too ("data-highlighted" as "datahighlighted"), so it is found by its full name.</summary>
    private static IEnumerable<string> Hooks(string[] hooks)
        => hooks.Select(hook => Normalize(hook.Split('=')[0]));

    /// <summary>A PascalCase name as its words, less the BitBh prefix: BitBhMenuCheckboxItem is menu, checkbox, item.</summary>
    private static string[] SplitName(string name)
        => [.. PascalWordRegex().Matches(name).Select(match => match.Value.ToLowerInvariant())
                                              .Where(word => word is not ("bit" or "bh" or "i"))];

    private static string[] Words(string text)
    {
        var words = WordRegex().Matches(text).Select(match => match.Value).ToList();

        // A hyphenated hook is also one word, so "data-state" and "datastate" both find it.
        words.AddRange(HyphenatedRegex().Matches(text).Select(match => match.Value.Replace("-", string.Empty, StringComparison.Ordinal)));

        return [.. words.SelectMany(word => word.Any(char.IsUpper) && word.Any(char.IsLower) ? [word.ToLowerInvariant(), .. SplitName(word)] : new[] { word.ToLowerInvariant() })
                        .Where(word => word.Length > 1)];
    }

    private static string Normalize(string text)
        => BlazorHeadlessUISuggest.Normalize(text);

    [GeneratedRegex(@"[A-Za-z0-9]+")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"[A-Za-z0-9]+(?:-[A-Za-z0-9]+)+")]
    private static partial Regex HyphenatedRegex();

    [GeneratedRegex(@"[A-Z]?[a-z0-9]+|[A-Z]+(?![a-z])")]
    private static partial Regex PascalWordRegex();
}
