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
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted)
    {
        /// <summary>
        /// The title split into words, camel-case humps included, so "updateOnVisibility" is found
        /// by "visibility" - and so a query word only counts as a title hit when it IS one of those
        /// words, rather than merely appearing inside one.
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
            score += (isTitleWord ? 12 : 0) + inTitle * 3 + inBoosted * 5 + Math.Min(inBody, 6);
        }

        // Every term matching is the strongest signal a hit is the right one.
        return matched == 0 ? 0 : score * matched;
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

            // A capital after a lowercase letter starts a new word ("updateOnVisibility" -> update
            // on visibility), while a run of capitals stays together ("URL", "SRI").
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

        foreach (var section in BswupSourceCatalog.GuideSections)
        {
            var body = BswupSourceCatalog.GetGuideSection(section.Heading) ?? string.Empty;

            entries.Add(new Entry("Guide section", section.Heading, section.Parent,
                $"GetBswupGuideSection(heading: \"{section.Heading}\")", body, string.Empty));
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
                $"GetBswupSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty));
        }

        return [.. entries];
    }
}
