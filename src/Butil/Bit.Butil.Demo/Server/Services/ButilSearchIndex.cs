using Bit.Butil.Demo.Client.Docs;
using Bit.Butil.Demo.Server.Dtos;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, the docs
/// pages, every public type and member, the browser-support matrix and the demo's source files.
/// <para>
/// Butil wraps around sixty browser APIs, and the name a caller reaches for is rarely the name the
/// platform chose - "copy to clipboard" is <c>Clipboard.WriteText</c>, "am I online" is
/// <c>NetworkInformation</c>, "keep the screen on" is <c>WakeLock</c>. Guessing which of the
/// corpora holds the answer, and what it is called there, is the actual work; this does it once.
/// Each hit therefore carries the exact follow-up tool call that returns its full text, so one
/// search is enough to know what to ask for next.
/// </para>
/// </summary>
public static class ButilSearchIndex
{
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted)
    {
        /// <summary>
        /// The title split into words, camel-case humps included, so "WriteText" is found by
        /// "write text" - and so a query word only counts as a title hit when it IS one of those
        /// words, rather than merely appearing inside one ("cache" inside "CacheStorage").
        /// </summary>
        public string[] TitleWords { get; } = SplitWords(Title);
    }

    private const int MaxTerms = 16;

    private static readonly Lazy<Entry[]> _entries = new(Build);

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto",
        // Every entry in this index is about the browser, in Blazor, through Butil - a term that
        // matches everything ranks nothing.
        "butil", "blazor", "browser", "javascript"
    };

    /// <summary>
    /// Builds the index ahead of the first query. Reflecting over the whole assembly and walking
    /// every catalog takes long enough to be noticed, and the caller who happens to be first is the
    /// one who would wait for it; startup has the time to spare.
    /// </summary>
    public static void Warm() => _ = _entries.Value;

    public static ButilSearchHitDto[] Search(string query, int limit)
    {
        var terms = Tokenize(query);
        if (terms.Length == 0) return [];

        return [.. _entries.Value
            .Select(entry => (Entry: entry, Score: Score(entry, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(hit => new ButilSearchHitDto
            {
                Kind = hit.Entry.Kind,
                Title = hit.Entry.Title,
                Context = hit.Entry.Context,
                Tool = hit.Entry.Tool,
                Snippet = Snippet(hit.Entry.Body, terms)
            })];
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

            // A term in a name is worth far more than the same term buried in prose: someone asking
            // for "ReadText" wants the method, not the paragraphs that happen to mention it.
            score += (isTitleWord ? 12 : 0) + inTitle * 3 + inBoosted * 5 + Math.Min(inBody, 6);
        }

        // Every term matching is the strongest signal a hit is the right one.
        return matched == 0 ? 0 : score * matched;
    }

    /// <summary>
    /// Same word, plural aside - "cookie" has to find "Cookies", and nobody phrases a question in
    /// the number the API happens to use.
    /// </summary>
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

        var start = Math.Max(0, index - 80);
        var length = Math.Min(240, body.Length - start);
        var snippet = body.Substring(start, length).Replace('\n', ' ').Replace('\r', ' ').Trim();

        return $"{(start > 0 ? "..." : null)}{snippet}{(start + length < body.Length ? "..." : null)}";
    }

    /// <summary>Splits a name or heading into words, breaking camel-case humps as well as punctuation.</summary>
    private static string[] SplitWords(string text)
    {
        var words = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) is false)
            {
                if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
                continue;
            }

            // A capital after a lowercase letter starts a new word ("WriteText" -> write text),
            // while a run of capitals stays together ("URL", "NFC").
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

        return [.. query.Split(['.', ',', ';', ':', '?', '!', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '<', '>', '-', '_', ' ', '\t', '\n', '\r'],
                              StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // One- and two-letter words ("a", "in", "do") match everything and rank nothing - and
            // the words a question is phrased with do worse than nothing: "how do I read FROM the
            // clipboard" would otherwise score every entry whose text contains "from".
            .Where(term => term.Length > 2 && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            // Every term is counted in every entry's body, so the work is terms x corpus. No question
            // is phrased in more words than this, while a pasted file as a query would scan for hours.
            .Take(MaxTerms)];
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>(4096);

        foreach (var section in ButilSourceCatalog.GuideSections)
        {
            var body = ButilSourceCatalog.GetGuideSection(section.Heading) ?? string.Empty;

            entries.Add(new Entry("Guide section", section.Heading, section.Parent,
                $"GetButilGuideSection(heading: \"{section.Heading}\")", body, string.Empty));
        }

        foreach (var group in DocsNav.Groups)
        {
            foreach (var link in group.Links)
            {
                // The types behind a page are boosted, so "LocalStorage" finds the page titled
                // "Local & Session Storage" - which does not contain the word at all.
                entries.Add(new Entry("Docs page", link.Title, group.Title,
                    $"GetButilDocsPage(slug: \"{link.Url}\")", link.Summary,
                    $"{link.Url} {string.Join(' ', link.TypeNames())}"));
            }
        }

        foreach (var capability in ButilCapabilityCatalog.Capabilities)
        {
            entries.Add(new Entry("Browser support", capability.Api, capability.BrowserSupport,
                $"InspectButilApi(name: \"{capability.Services.FirstOrDefault() ?? capability.Api}\")",
                string.Join(' ', capability.Requires), string.Join(' ', capability.Services)));
        }

        foreach (var type in ButilApiCatalog.Types)
        {
            entries.Add(new Entry($"API {type.Kind.ToLowerInvariant()}", type.Name, null,
                $"GetButilApiDetails(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = ButilApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            foreach (var member in details.Members)
            {
                entries.Add(new Entry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetButilApiDetails(typeName: \"{type.Name}\")",
                    $"{member.Summary} {member.Remarks}".Trim(),
                    // The default of a const carries the answer for the string catalogs: someone
                    // searching for the event name "pointerdown" wants ButilEvents.PointerDown.
                    $"{member.Type} {member.Signature} {member.Default}".Trim()));
            }
        }

        foreach (var file in ButilSourceCatalog.SourceFiles)
        {
            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetButilSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty));
        }

        return [.. entries];
    }
}
