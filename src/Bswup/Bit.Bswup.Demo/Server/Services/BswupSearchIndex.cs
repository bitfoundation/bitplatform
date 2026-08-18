using Bit.Bswup.Demo.Client;
using Bit.Bswup.Demo.Server.Dtos;

namespace Bit.Bswup.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, the docs
/// pages, every script attribute and service-worker setting, the lifecycle messages, the
/// JavaScript API and the source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there -
/// "the app never picks up new versions" is a script attribute, a JavaScript API call, a docs page
/// and a hosting-header recipe all at once. Each hit therefore carries the exact follow-up tool
/// call that returns the full text, so one search is enough to know what to ask for next.
/// </para>
/// </summary>
public static class BswupSearchIndex
{
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted, int Weight = ReferenceWeight)
    {
        /// <summary>
        /// The words the title is found by: what punctuation leaves behind AND those pieces split
        /// at their camel-case humps, so "self.assetsExclude" answers to "assetsExclude" and to
        /// "exclude" alike - and so a query word only counts as a title hit when it IS one of those
        /// words, rather than merely appearing inside one.
        /// </summary>
        public string[] TitleWords { get; } = SplitWords(Title);
    }

    private const int MaxTerms = 16;

    /// <summary>The weight of an entry that answers a question: a setting, an event, a page.</summary>
    private const int ReferenceWeight = 10;

    /// <summary>
    /// The weight of a source file. They are examples, not answers, and their titles are paths
    /// whose segments ("Client", "Pages", "Shared") are common words carrying no topic - without
    /// this every question phrased with one of them is answered with a directory listing.
    /// </summary>
    private const int ExampleWeight = 4;

    /// <summary>
    /// The weight of a guide section that is most of the guide. The README's first heading covers
    /// two thirds of the file, so it matches nearly every query and answers none of them with more
    /// than "read the guide". It stays findable; anything precise outranks it.
    /// </summary>
    private const int BroadSectionWeight = 4;

    /// <summary>A section holding more than this fraction of the guide counts as one of those.</summary>
    private const int BroadSectionFraction = 4;

    /// <summary>
    /// The most an entry can earn from prose, however much of it there is. A long section mentions
    /// everything, so without a ceiling the biggest document in the corpus is the top hit for
    /// every query - and a name match, which is the far stronger signal, never gets ahead of it.
    /// </summary>
    private const int MaxBodyScore = 8;

    private static readonly Lazy<Entry[]> _entries = new(Build);

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto"
    };

    public static BswupSearchHitDto[] Search(string query, int limit)
    {
        var terms = Tokenize(query);
        if (terms.Length == 0) return [];

        return [.. _entries.Value
            .Select(entry => (Entry: entry, Score: Score(entry, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(hit => new BswupSearchHitDto
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
        var body = 0;
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
            // for "stallTimeout" wants the option, not the paragraphs that happen to mention it.
            score += (isTitleWord ? 12 : 0) + (inTitle * 3) + (inBoosted * 5);

            // Prose is counted apart from the names so it can be capped as a whole (see MaxBodyScore).
            body += Math.Min(inBody, 6);
        }

        // Every term matching is the strongest signal a hit is the right one.
        return matched == 0 ? 0 : (score + Math.Min(body, MaxBodyScore)) * matched * entry.Weight;
    }

    /// <summary>
    /// Same word, plural aside - "event" has to find the page called "Events &amp; Handler", and
    /// nobody phrases a question in the number the heading happens to use.
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

    /// <summary>
    /// Splits a name or heading into the words it should be found by: the pieces punctuation leaves
    /// behind, AND those pieces broken at their camel-case humps.
    /// <para>
    /// Both are needed, and keeping only the humps is the trap: "stallTimeout" would then be known
    /// as "stall" and "Timeout" and nothing else, so someone typing the option's own name matches
    /// no word of its title at all - and the docs page that merely lists the name among its
    /// keywords outranks the option itself.
    /// </para>
    /// </summary>
    private static string[] SplitWords(string text)
    {
        var words = new List<string>();
        var current = new System.Text.StringBuilder();
        var segment = new System.Text.StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) is false)
            {
                Flush();
                continue;
            }

            // A capital after a lowercase letter starts a new word ("updateOnVisibility" -> update
            // on visibility), while a run of capitals stays together ("URL", "SRI"). The segment
            // keeps accumulating across the hump, so the undivided name survives alongside them.
            if (char.IsUpper(c) && current.Length > 0 && char.IsLower(current[^1]))
            {
                words.Add(current.ToString());
                current.Clear();
            }

            current.Append(c);
            segment.Append(c);
        }

        Flush();

        return [.. words.Distinct(StringComparer.OrdinalIgnoreCase)];

        void Flush()
        {
            if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
            if (segment.Length > 0) { words.Add(segment.ToString()); segment.Clear(); }
        }
    }

    private static string[] Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        return [.. query.Split(['.', ',', ';', ':', '?', '!', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '<', '>', '-', '_', ' ', '\t', '\n', '\r'],
                              StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // One- and two-letter words ("a", "in", "do") match everything and rank nothing - and
            // the words a question is phrased with do worse than nothing.
            .Where(term => term.Length > 2 && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            // Every term is counted in every entry's body, so the work is terms x corpus. No question
            // is phrased in more words than this, while a pasted file as a query would scan for hours.
            .Take(MaxTerms)];
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>(256);

        var guideLines = Math.Max(1, BswupSourceCatalog.Readme.Count(c => c == '\n'));

        foreach (var section in BswupSourceCatalog.GuideSections)
        {
            var body = BswupSourceCatalog.GetGuideSection(section.Heading) ?? string.Empty;

            var weight = section.Lines * BroadSectionFraction > guideLines ? BroadSectionWeight : ReferenceWeight;

            entries.Add(new Entry("Guide section", section.Heading, section.Parent,
                $"GetBswupGuideSection(heading: \"{section.Heading}\")", body, string.Empty, weight));
        }

        foreach (var page in DocsCatalog.Sections.SelectMany(s => s.Pages.Select(p => (Section: s.Title, Page: p))))
        {
            entries.Add(new Entry("Docs page", page.Page.Title, page.Section,
                $"GetBswupDocsPage(slug: \"{page.Page.Slug}\")", page.Page.Description, page.Page.Keywords));
        }

        foreach (var option in BswupScriptCatalog.ScriptOptions)
        {
            entries.Add(new Entry("Script attribute", option.Name, "bit-bswup.js script tag",
                "GetBswupScriptOptions()", $"{option.Summary} {option.Remarks}".Trim(), $"{option.Type} {option.Default}".Trim()));
        }

        foreach (var setting in BswupScriptCatalog.WorkerSettings)
        {
            entries.Add(new Entry("Service worker setting", $"self.{setting.Name}", "service-worker.js",
                "GetBswupServiceWorkerSettings()", $"{setting.Summary} {setting.Remarks}".Trim(), $"{setting.Type} {setting.Default}".Trim()));
        }

        foreach (var mode in BswupScriptCatalog.Modes)
        {
            entries.Add(new Entry("Service worker mode", $"self.mode = '{mode.Name}'", "preset",
                "GetBswupServiceWorkerModes()",
                string.Join(", ", mode.Settings.Select(setting => $"{setting.Key} = {setting.Value}")), mode.Name));
        }

        foreach (var message in BswupScriptCatalog.Events)
        {
            entries.Add(new Entry("Event", message.Name, message.Message,
                "GetBswupEvents()", $"{message.Summary} {message.Deprecated}".Trim(), $"{message.Message} {message.Payload}".Trim()));
        }

        foreach (var member in BswupScriptCatalog.JsApi)
        {
            entries.Add(new Entry("JavaScript API", $"BitBswup.{member.Name}", "page script",
                "GetBswupJsApi()", member.Summary ?? string.Empty, member.Signature));
        }

        foreach (var parameter in BswupProgressCatalog.Parameters)
        {
            entries.Add(new Entry("Progress parameter", $"BswupProgress.{parameter.Name}", "built-in progress UI",
                "GetBswupProgressUI()", parameter.Summary ?? string.Empty, $"{parameter.Type} {parameter.Default}".Trim()));
        }

        foreach (var element in BswupProgressCatalog.ProgressUi.Elements)
        {
            entries.Add(new Entry("Progress element", $"#{element.Id}", "splash markup",
                "GetBswupProgressUI()", element.Role, string.Empty));
        }

        foreach (var file in BswupSourceCatalog.SourceFiles)
        {
            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetBswupSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, ExampleWeight));
        }

        return [.. entries];
    }
}
