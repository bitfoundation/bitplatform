using System.Text;
using Bit.Blex.Demo.Server.Dtos;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, the docs pages,
/// every public type and member, the generator's diagnostics and the demo's source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there - "how do
/// I cancel a running request?" is a guide section, a docs page, an attribute property and a demo store
/// all at once. Each hit therefore carries the exact follow-up tool call that returns the full text, so
/// one search is enough to know what to ask for next.
/// </para>
/// </summary>
public static class BlexSearchIndex
{
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted, int Weight = ReferenceWeight)
    {
        /// <summary>
        /// The title split into words, camel-case humps included, so "DebounceMilliseconds" is found by
        /// "debounce" - and a query word only counts as a title hit when it IS one of those words.
        /// </summary>
        public string[] TitleWords { get; } = SplitWords(Title);
    }

    private const int MaxTerms = 16;

    /// <summary>The most hits one search may return, whatever the caller asked for.</summary>
    private const int MaxHits = 50;

    private const int MaxSuggestions = 8;

    /// <summary>The weight of an entry that answers a question: a guide section, a page, a member, a diagnostic.</summary>
    private const int ReferenceWeight = 10;

    /// <summary>
    /// The weight of a source file. They are examples, not answers, and their titles are paths whose
    /// segments ("Client", "Stores") are common words carrying no topic.
    /// </summary>
    private const int ExampleWeight = 4;

    /// <summary>The most an entry can earn from prose, so the biggest document is not the top hit for every query.</summary>
    private const int MaxBodyScore = 8;

    private static readonly Lazy<Entry[]> _entries = new(Build);

    /// <summary>
    /// The words every entry here carries because of what the index is about. They separate nothing, so
    /// they are dropped - unless they are all the query said.
    /// </summary>
    private static readonly HashSet<string> _ambientWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "blex", "bit", "blazor", "state", "management"
    };

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto"
    };

    /// <summary>
    /// The words a caller from another state library uses for a Blex concept. They are matched as extra
    /// vocabulary of the entries that answer them, so "reducer" finds actions and "selector" finds the
    /// subscription APIs.
    /// </summary>
    private static readonly Dictionary<string, string> _synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Actions"] = "reducer reducers dispatch mutation mutate",
        ["Effects"] = "async side effect thunk saga fetch http loading",
        ["Computed state"] = "derived getter memoized memo selector",
        ["Components & selectors"] = "rerender re-render subscribe subscription selector",
        ["Persistence"] = "localstorage sessionstorage save reload rehydrate hydrate",
        ["Undo & redo"] = "history undo redo",
        ["DevTools & time travel"] = "redux devtools timetravel debugger",
        ["Entity adapter"] = "normalized collection crud list",
        ["Middleware"] = "interceptor logger logging veto",
    };

    public static BlexSearchResultDto Search(string? query, int limit)
    {
        var terms = Tokenize(query);

        if (terms.Length == 0)
        {
            return new BlexSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held no word longer than two letters that was not a filler word, so there was " +
                          "nothing to rank by. Search for the thing itself - 'effect cancel', 'persist', 'selector' - " +
                          "rather than for a sentence."
            };
        }

        var ranked = _entries.Value
            .Select(entry => (Entry: entry, Score: Score(entry, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            // Two entries read through the same call under the same name are one hit: a method with two
            // overloads is two members and one place to read about them.
            .DistinctBy(hit => hit.Entry.Tool.EndsWith("()", StringComparison.Ordinal) ? (string.Empty, hit.Entry.Tool) : (hit.Entry.Title, hit.Entry.Tool))
            .ToArray();

        var take = Math.Clamp(limit, 1, MaxHits);

        var hits = ranked.Take(take).Select(hit => new BlexSearchHitDto
        {
            Kind = hit.Entry.Kind,
            Title = hit.Entry.Title,
            Context = hit.Entry.Context,
            Tool = hit.Entry.Tool,
            Snippet = Snippet(hit.Entry.Body, terms)
        }).ToArray();

        if (hits.Length > 0)
        {
            return new BlexSearchResultDto
            {
                Hits = hits,
                HasMore = ranked.Length > hits.Length
            };
        }

        // A search that found nothing is where an agent is most likely to conclude the library has no
        // such feature and hand-roll one. Whatever came closest is named instead.
        var nearby = Suggest(terms);

        return new BlexSearchResultDto
        {
            Terms = terms,
            Hits = [],
            Message = nearby.Length > 0
                ? $"Nothing matched {string.Join(" + ", terms)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", terms)}. Try one word rather than several, or call " +
                   "GetBlexGuideSection, GetBlexDocsPage or GetBlexApi with no argument at all for the index of what " +
                   "there is. A concept Blex does not name is often the same idea under another word - a 'reducer' is an " +
                   "action, a 'thunk' is an effect, a 'getter' is a computed value.",
            DidYouMean = nearby.Length > 0 ? nearby : null
        };
    }

    private static string[] Suggest(string[] terms)
    {
        return [.. _entries.Value
            .Where(entry => terms.Any(term => term.Length >= 4
                                              && entry.TitleWords.Any(word => word.StartsWith(term, StringComparison.OrdinalIgnoreCase)
                                                                              || term.StartsWith(word, StringComparison.OrdinalIgnoreCase))))
            .Select(entry => entry.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestions)];
    }

    private static int Score(Entry entry, string[] terms)
    {
        var score = 0;
        var matched = 0;

        foreach (var term in terms)
        {
            var isTitleWord = entry.TitleWords.Any(word => Equivalent(word, term));
            var inTitle = isTitleWord ? 0 : Count(entry.Title, term);
            var inBoosted = Count(entry.Boosted, term);
            var inBody = Count(entry.Body, term);

            if (isTitleWord is false && inTitle + inBoosted + inBody == 0) continue;

            matched++;

            // A term in a name is worth far more than the same term buried in prose.
            score += (isTitleWord ? 12 : 0) + inTitle * 3 + inBoosted * 5 + Math.Min(inBody, MaxBodyScore);
        }

        return matched == 0 ? 0 : score * matched * entry.Weight;
    }

    /// <summary>Same word, plural aside - "effect" has to find the page called "Effects".</summary>
    private static bool Equivalent(string word, string term)
    {
        return string.Equals(word, term, StringComparison.OrdinalIgnoreCase)
            || string.Equals(word, $"{term}s", StringComparison.OrdinalIgnoreCase)
            || string.Equals($"{word}s", term, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string text, string term)
    {
        if (text.Length == 0) return 0;

        var count = 0;
        var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);

        while (index >= 0)
        {
            count++;
            index = text.IndexOf(term, index + term.Length, StringComparison.OrdinalIgnoreCase);
        }

        return count;
    }

    private static string Snippet(string body, string[] terms)
    {
        if (body.Length == 0) return string.Empty;

        var index = -1;
        foreach (var term in terms)
        {
            index = body.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index >= 0) break;
        }

        if (index < 0) index = 0;

        // Enough of the surrounding text to tell the right hit from the wrong one, and no more.
        var start = Math.Max(0, index - 60);
        var length = Math.Min(180, body.Length - start);
        var snippet = body.Substring(start, length).Replace('\n', ' ').Replace('\r', ' ').Trim();

        return $"{(start > 0 ? "..." : null)}{snippet}{(start + length < body.Length ? "..." : null)}";
    }

    /// <summary>Splits a name or heading into words, breaking camel-case humps as well as punctuation.</summary>
    private static string[] SplitWords(string text)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) is false)
            {
                if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
                continue;
            }

            if (char.IsUpper(c) && current.Length > 0 && char.IsLower(current[^1]))
            {
                words.Add(current.ToString());
                current.Clear();
            }

            current.Append(c);
        }

        if (current.Length > 0) words.Add(current.ToString());

        return [.. words];
    }

    private static string[] Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        string[] terms = [.. query.Split(['.', ',', ';', ':', '?', '!', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '<', '>', '-', '_', ' ', '\t', '\n', '\r'],
                              StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length > 2 && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTerms)];

        var meaningful = terms.Where(term => _ambientWords.Contains(term) is false).ToArray();

        return meaningful.Length > 0 ? meaningful : terms;
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>(512);

        foreach (var section in BlexSourceCatalog.GuideSections)
        {
            var body = BlexSourceCatalog.GetGuideSection(section.Heading) ?? string.Empty;

            entries.Add(new Entry("Guide section", section.Heading, section.Parent,
                $"GetBlexGuideSection(heading: \"{section.Heading}\")", body, string.Empty));
        }

        foreach (var page in DocsPageRenderer.Pages)
        {
            entries.Add(new Entry("Docs page", page.Title, page.Section,
                $"GetBlexDocsPage(slug: \"{page.Slug}\")", page.Summary, _synonyms.GetValueOrDefault(page.Title) ?? string.Empty));
        }

        foreach (var type in BlexApiCatalog.Types)
        {
            entries.Add(new Entry($"API {type.Kind.ToLowerInvariant()}", type.Name, type.Package,
                $"GetBlexApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = BlexApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: two overloads are one thing to know about, and
            // both are read by the same call anyway.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();

                entries.Add(new Entry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBlexApi(typeName: \"{type.Name}\")",
                    string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim(),
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim()));
            }
        }

        foreach (var diagnostic in BlexDiagnosticsReference.Entries)
        {
            entries.Add(new Entry("Diagnostic", $"{diagnostic.Id} {diagnostic.Title}", diagnostic.Severity,
                $"GetBlexDiagnostics(code: \"{diagnostic.Id}\")",
                $"{diagnostic.MessageFormat} {diagnostic.Doc?.Cause}".Trim(),
                $"{diagnostic.Id} build error compile generator"));
        }

        foreach (var file in BlexSourceCatalog.SourceFiles)
        {
            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetBlexSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, ExampleWeight));
        }

        return [.. entries];
    }
}
