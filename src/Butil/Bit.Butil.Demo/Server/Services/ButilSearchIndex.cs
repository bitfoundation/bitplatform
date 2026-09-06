using Bit.Butil.Demo.Client.Docs;
using Bit.Butil.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, the docs
/// pages, every public type and member, the browser-support matrix and the demo's source files.
/// <para>
/// Butil wraps around seventy browser APIs, and the name a caller reaches for is rarely the name the
/// platform chose - "copy to clipboard" is <c>Clipboard.WriteText</c>, "am I online" is
/// <c>NetworkInformation</c>, "keep the screen on" is <c>WakeLock</c>. Guessing which of the
/// corpora holds the answer, and what it is called there, is the actual work; this does it once.
/// Each hit therefore carries the exact follow-up tool call that returns its full text, so one
/// search is enough to know what to ask for next.
/// </para>
/// </summary>
public static partial class ButilSearchIndex
{
    /// <param name="Body">The text this entry is matched against.</param>
    /// <param name="Prose">
    /// What a hit on this entry quotes back, when that is not the text it was matched against. A
    /// docs page is indexed as the Razor component that renders it, because the prose an agent is
    /// searching for lives in that component's attributes - but a window cut out of the markup is
    /// unreadable, and a hit is read by whoever asked for it. Null means the body is already prose.
    /// </param>
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted, string? Prose = null)
    {
        /// <summary>
        /// The title split into words, camel-case humps included, so "WriteText" is found by
        /// "write text" - and so a query word only counts as a title hit when it IS one of those
        /// words, rather than merely appearing inside one ("cache" inside "CacheStorage").
        /// </summary>
        public string[] TitleWords { get; } = [.. SplitWords(Title).Select(word => word.ToLowerInvariant())];

        /// <summary>
        /// The searchable text lowered once, when the index is built, so scoring can scan it with
        /// ordinal comparisons.
        /// <para>
        /// Every query term is counted in every entry, and a case-insensitive scan pays to fold each
        /// character it walks past - over a corpus that includes whole source files, on every search.
        /// Folding once at startup trades a second copy of the text for a scan that is a plain byte
        /// comparison. <see cref="Snippet"/> deliberately keeps reading the original: it returns the
        /// text to the caller, who should see it in the case it was written in.
        /// </para>
        /// </summary>
        public string LoweredTitle { get; } = Title.ToLowerInvariant();

        /// <inheritdoc cref="LoweredTitle"/>
        public string LoweredBoosted { get; } = Boosted.ToLowerInvariant();

        /// <inheritdoc cref="LoweredTitle"/>
        public string LoweredBody { get; } = Body.ToLowerInvariant();
    }

    private const int MaxTerms = 16;

    /// <summary>The page that documents this MCP server - see where it is skipped in <see cref="Build"/>.</summary>
    private const string McpServerPageSlug = "mcp-server";

    // PublicationOnly rather than the default: the default mode caches the exception a failed build
    // threw and rethrows it for the lifetime of the process, so one transient failure would leave
    // SearchButil permanently broken. Here a failed build is simply retried by the next caller, at
    // the price of two callers racing being able to build it twice - which costs time, not
    // correctness, since the index is the same array either way.
    private static readonly Lazy<Entry[]> _entries = new(Build, LazyThreadSafetyMode.PublicationOnly);

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

    /// <summary>
    /// Whether a query still holds a term after the stop words and the short words are dropped.
    /// "how do I get the browser" does not, and an empty result for it means something different
    /// from an empty result for "quantum clipboard".
    /// </summary>
    public static bool IsSearchable(string? query) => Tokenize(query).Length > 0;

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
                Snippet = Snippet(hit.Entry.Prose ?? hit.Entry.Body, terms)
            })];
    }

    private static int Score(Entry entry, string[] terms)
    {
        var score = 0;
        var body = 0;
        var matched = 0;

        foreach (var term in terms)
        {
            var isTitleWord = entry.TitleWords.Any(word => Equivalent(word, term));
            var inTitle = isTitleWord ? 0 : Count(entry.LoweredTitle, term);
            var inBoosted = Count(entry.LoweredBoosted, term);
            var inBody = Count(entry.LoweredBody, term);

            if (isTitleWord is false && inTitle + inBoosted + inBody == 0) continue;

            matched++;

            // A term in a name is worth far more than the same term buried in prose: someone asking
            // for "ReadText" wants the method, not the paragraphs that happen to mention it.
            score += (isTitleWord ? 12 : 0) + inTitle * 3 + inBoosted * 5;
            body += Math.Min(inBody, 3);
        }

        // A body here is a whole document - a guide section, a docs page and the source it is
        // written in - and a long enough document mentions everything. Capping the prose as a whole
        // rather than per term is what lets full text be indexed at all: it can still rank a hit and
        // break a tie between two of them, but it can never outweigh the entry whose NAME was asked
        // for, which is the ranking the entire index exists to produce.
        score += Math.Min(body, 9);

        // Every term matching is the strongest signal a hit is the right one.
        return matched == 0 ? 0 : score * matched;
    }

    /// <summary>
    /// Same word, plural aside - "cookie" has to find "Cookies", and nobody phrases a question in
    /// the number the API happens to use. Both sides are already lowered - the title words when the
    /// entry was built, the term when the query was tokenized - so this compares ordinally.
    /// </summary>
    private static bool Equivalent(string word, string term)
    {
        return string.Equals(word, term, StringComparison.Ordinal)
            || (word.Length == term.Length + 1 && word[^1] == 's' && string.Equals(word[..^1], term, StringComparison.Ordinal))
            || (term.Length == word.Length + 1 && term[^1] == 's' && string.Equals(term[..^1], word, StringComparison.Ordinal));
    }

    /// <summary>
    /// How often a term occurs in one of an entry's lowered fields. Ordinal on purpose: the text was
    /// lowered when the index was built and the term when the query was tokenized, so the fold has
    /// already been paid for and this walks bytes.
    /// </summary>
    private static int Count(string text, string term)
    {
        if (text.Length == 0) return 0;

        var count = 0;
        var index = text.IndexOf(term, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = text.IndexOf(term, index + term.Length, StringComparison.Ordinal);
        }

        return count;
    }

    /// <summary>
    /// The window of the ORIGINAL body around the first term that occurs in it - what the caller
    /// reads, so it keeps the case it was written in. One scan per hit that is actually returned,
    /// rather than one per entry in the index, which is why this can afford to fold as it goes.
    /// </summary>
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
            // Lowered here, once per query, so every comparison downstream can be ordinal against
            // the entry text that was lowered when the index was built.
            .Select(term => term.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            // Every term is counted in every entry's body, so the work is terms x corpus. No question
            // is phrased in more words than this, while a pasted file as a query would scan for hours.
            .Take(MaxTerms)];
    }

    /// <summary>
    /// The text of a docs page: the source of the component that renders it.
    /// <para>
    /// A page's own summary is one sentence, and an index of nothing but one-sentence summaries only
    /// ever finds the page whose title someone already knew. The prose that answers a question -
    /// what the API is for, which member does what, the caveat halfway down - lives in the page,
    /// and so does every code sample on it. The source is the static copy of all of that: the
    /// rendered Markdown would be truer, but rendering needs a request scope and a live renderer,
    /// which an index built once at startup has neither of.
    /// </para>
    /// <para>
    /// The markup around the prose (attribute names, CSS classes) rides along as noise. It is
    /// harmless here because a body can only ever contribute a capped number of points - see
    /// <see cref="Score"/> - so matching markup can rank a page but never win on it.
    /// </para>
    /// </summary>
    private static string PageText(DocLink link)
        => ButilSourceCatalog.GetSourceFile($"Demo/Client/Pages/{link.PageType.Name}.razor") ?? string.Empty;

    /// <summary>
    /// The prose of a docs page, for a hit to quote: the page's own summary, then every long quoted
    /// string in the component that renders it.
    /// <para>
    /// Those quoted strings are the documentation - a page writes its lead and each section's
    /// explanation as attribute values - while everything shorter is a CSS class, an element name or
    /// an identifier. Taking only the long ones leaves sentences, which is what a snippet is for;
    /// the markup stays in <see cref="Entry.Body"/>, where it can still be matched.
    /// </para>
    /// </summary>
    private static string PageProse(DocLink link)
    {
        var source = PageText(link);
        var prose = new System.Text.StringBuilder(link.Summary);

        // Attribute values only - the raw string literals a page writes its code samples in are not
        // prose, and they are quoted with """ rather than with a single ".
        foreach (Match match in QuotedProseRegex().Matches(source))
        {
            var text = match.Groups["text"].Value.Trim();

            if (IsProse(text)) prose.Append(' ').Append(text);
        }

        return prose.ToString();
    }

    /// <summary>
    /// A sentence rather than a value that happened to be quoted. Long enough to be prose, with
    /// spaces in it, and none of the punctuation that gives away markup or a line of code - a raw
    /// string holding a code sample is delimited by three quotes, so its lines arrive here too.
    /// </summary>
    private static bool IsProse(string text)
    {
        return text.Length >= 40
            && text.Contains(' ', StringComparison.Ordinal)
            && text.IndexOfAny(['<', '>', '{', '}', ';', '=']) < 0;
    }

    /// <summary>A double-quoted value in a Razor component - which is where a docs page's prose is written.</summary>
    [GeneratedRegex("\"(?<text>[^\"\n]+)\"")]
    private static partial Regex QuotedProseRegex();

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
                // The page documenting this server is left out of the corpus it serves: it quotes
                // example queries and every tool name, so it matches questions about the library
                // itself - and what it explains is what the client was handed at initialize.
                if (string.Equals(link.Url, McpServerPageSlug, StringComparison.Ordinal)) continue;

                // The types behind a page are boosted, so "LocalStorage" finds the page titled
                // "Local & Session Storage" - which does not contain the word at all.
                entries.Add(new Entry("Docs page", link.Title, group.Title,
                    $"GetButilDocsPage(slug: \"{link.Url}\")", $"{link.Summary}\n{PageText(link)}",
                    $"{link.Url} {string.Join(' ', link.TypeNames())}", PageProse(link)));
            }
        }

        foreach (var capability in ButilCapabilityCatalog.Capabilities)
        {
            entries.Add(new Entry("Browser support", capability.Api, capability.BrowserSupport,
                $"PlanButilFeature(apis: \"{capability.Services.FirstOrDefault() ?? capability.Api}\")",
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
