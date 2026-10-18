using System.Text;
using Bit.Bmage.Demo.Server.Dtos;
using Bit.Bmage.Demo.Client.Shared;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: every section of every guide, the
/// setup guides, every public type and member of the three packages, the recipes, the review rules,
/// the demo pages and their sources.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there.
/// "Make an avatar under 100 KB" is a recipe (avatar-crop), a guide section (Quality and size
/// budgets), an options property (ImageExportOptions.MaxFileSize), a demo page (/avatar) and a tool
/// that measures it (RunBmagePipeline) all at once. Each hit therefore carries the exact follow-up
/// call that returns its full text, so one search is enough to know what to ask for next.
/// </para>
/// </summary>
public static class BmageSearchIndex
{
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted)
    {
        /// <summary>
        /// The title split into words, camel-case humps included, so "MaxFileSize" is found by "file"
        /// - and so a query word only counts as a title hit when it IS one of those words.
        /// </summary>
        public string[] TitleWords { get; } = SplitWords(Title);

        /// <summary>The name without the type that owns it: "Cover" out of "ImagePipeline.Cover".</summary>
        public string LocalName { get; } = Title[(Title.LastIndexOf('.') + 1)..];
    }

    private const int MaxTerms = 16;

    // Not readonly: a Lazy that faulted would answer every later search with the same exception.
    // Replacing it lets the next search build the index again.
    private static Lazy<Entry[]> _entries = new(Build);

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto",
        // Every entry here is about images in Bmage; a query saying so matches everything and ranks nothing.
        "bmage", "image", "images", "blazor", "picture",
        // The words a problem is reported in. Left in, they outrank the answer.
        "not", "why", "use", "using", "can", "will", "wont", "isnt", "doesnt", "instead", "still", "just", "only", "even",
        "work", "works", "working",
    };

    /// <summary>The best matches for a query, each with the call that returns it in full.</summary>
    public static BmageSearchHitDto[] Search(string? query, int limit)
    {
        var terms = Tokenize(query);
        if (terms.Length == 0) return [];

        var entries = GetEntries();

        return [.. entries
            .Select(entry => (Entry: entry, Score: Score(entry, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            // An inherited member documented once - TryFuse on every adjustment, say - is one answer,
            // not eight: left in, the copies push everything else off the page.
            .DistinctBy(hit => (hit.Entry.Kind, hit.Entry.LocalName, hit.Entry.Body))
            .Take(Math.Clamp(limit, 1, 50))
            .Select(hit => new BmageSearchHitDto
            {
                Kind = hit.Entry.Kind,
                Title = hit.Entry.Title,
                Context = hit.Entry.Context,
                Tool = hit.Entry.Tool,
                Snippet = Snippet(hit.Entry.Body, terms)
            })];
    }

    private static Entry[] GetEntries()
    {
        var lazy = _entries;

        try
        {
            return lazy.Value;
        }
        catch (Exception)
        {
            Interlocked.CompareExchange(ref _entries, new Lazy<Entry[]>(Build), lazy);

            throw;
        }
    }

    private static int Score(Entry entry, string[] terms)
    {
        var score = 0;
        var matched = 0;

        foreach (var term in terms)
        {
            var isTitleWord = Equivalent(entry.LocalName, term)
                           || entry.TitleWords.Any(word => Equivalent(word, term));
            var inTitle = isTitleWord ? 0 : Count(entry.Title, term);
            var inBoosted = Count(entry.Boosted, term);
            var inBody = Count(entry.Body, term);

            if (isTitleWord is false && inTitle + inBoosted + inBody == 0) continue;

            matched++;

            // A term in a name is worth far more than the same term buried in prose: someone asking
            // for "MaxFileSize" wants the property, not the paragraphs that mention it.
            score += (isTitleWord ? 12 : 0) + inTitle * 3 + inBoosted * 5 + Math.Min(inBody, 6);
        }

        // Every term matching is the strongest signal a hit is the right one.
        return matched == 0 ? 0 : score * matched;
    }

    /// <summary>Same word, plural aside - "crop" has to find "Crops", and "thumbnail" "thumbnails".</summary>
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
        var current = new StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) is false)
            {
                if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
                continue;
            }

            // A capital after a lowercase letter starts a new word ("MaxFileSize" -> max file size),
            // while a run of capitals stays together ("EXIF", "GPS").
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
            // One- and two-letter words match everything and rank nothing - except the few that are
            // the names of things here.
            .Where(term => (term.Length > 2 || term.Equals("ui", StringComparison.OrdinalIgnoreCase)) && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            // Every term is counted in every entry's body, so the work is terms x corpus.
            .Take(MaxTerms)];
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>(2048);

        foreach (var document in BmageSourceCatalog.Documents)
        {
            entries.Add(new Entry("Guide", document.Title, document.Name,
                $"GetBmageDocument(name: \"{document.Name}\")", document.Summary ?? string.Empty,
                $"{document.Name} {string.Join(' ', document.Sections)}"));

            foreach (var (heading, level) in BmageSourceCatalog.GetHeadings(document.Name))
            {
                var body = BmageSourceCatalog.GetDocumentSection(document.Name, heading) ?? string.Empty;

                entries.Add(new Entry("Guide section", heading, $"{document.Title}{(level == 3 ? " (sub-section)" : null)}",
                    $"GetBmageDocument(name: \"{document.Name}\", section: \"{heading}\")", body, string.Empty));
            }
        }

        // The setup guides carry the per-host facts - which registration, which containers, which
        // codecs - where the answer to "works in development, fails at prerender" lives.
        foreach (var mode in BmageSetupGuide.RenderModes)
        {
            entries.Add(new Entry("Setup guide", $"Setup for {mode}", "Hosting model",
                $"GetBmageSetupGuide(renderMode: \"{mode}\")", BmageSetupGuide.Get(mode) ?? string.Empty,
                $"{mode} install register services addbitbmageservices addbitbmageheadlessservices container prerender host program"));
        }

        // The tools that answer by running the engine. They are the right response to a class of
        // question - "how big will this be", "is this budget reachable", "does Preserve keep GPS" -
        // that names no type, section or recipe, and so would otherwise match nothing at all.
        entries.Add(new Entry("Tool", "Measure a pipeline: output size, bytes per format and quality, timings", "Runs the real engine",
            "RunBmagePipeline(pipeline: \"...\")",
            "Runs a pipeline on a generated photo, screenshot or graphic with the managed codecs and reports the output size, " +
            "the encoded bytes for each format and quality, a MaxFileSize search, the optimised operation list and the timings.",
            "bytes size kb file quality compression jpeg png budget maxfilesize compare formats slow fast timing performance optimiser optimizer measure"));

        entries.Add(new Entry("Tool", "Check what each metadata policy keeps (GPS, serial number, owner)", "Runs the real engine",
            "CompareBmageMetadataPolicies(format: \"jpeg\")",
            "Encodes a GPS-tagged camera photo under Remove, StripSensitive and Preserve with the real encoder and reads each file back.",
            "gps location privacy exif metadata strip remove preserve stripsensitive leak serial owner geotag"));

        entries.Add(new Entry("Tool", "Review Bmage code for runtime mistakes", "Checks code",
            "ReviewBmageCode(code: \"...\")",
            "Finds the mistakes that compile cleanly: browser calls during prerendering, undisposed buffers, missing registrations, " +
            "missing stylesheet, WebP without a fallback or on a server, Preserve leaking GPS.",
            "review check lint audit bug mistake wrong error"));

        foreach (var recipe in BmageRecipeCatalog.All)
        {
            entries.Add(new Entry("Recipe", recipe.Title, $"Copy-pasteable pattern ({recipe.RunsIn})",
                $"GetBmageRecipe(id: \"{recipe.Id}\")",
                $"{recipe.Intent}\n{recipe.Notes}", $"{recipe.Id} {recipe.Keywords}"));
        }

        foreach (var rule in BmageCodeReview.Catalog)
        {
            entries.Add(new Entry("Review rule", rule.Id, $"{rule.Severity}, from {rule.Source}",
                "ReviewBmageCode(code: \"...\")", rule.Catches, rule.Id.Replace('-', ' ')));
        }

        foreach (var type in BmageApiCatalog.Types)
        {
            entries.Add(new Entry($"API {type.Kind.ToLowerInvariant()}", type.Name, $"{type.Namespace} ({type.Package})",
                $"GetBmageApiDetails(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = BmageApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            foreach (var member in details.Members.Where(member => member.Kind != "Constructor").DistinctBy(member => member.Name))
            {
                entries.Add(new Entry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBmageApiDetails(typeName: \"{type.Name}.{member.Name}\")",
                    $"{member.Summary} {member.Remarks}".Trim(),
                    $"{member.Type} {member.Signature}".Trim()));
            }
        }

        foreach (var page in NavItem.All)
        {
            entries.Add(new Entry("Demo page", page.Title, "Live example",
                $"GetBmageSourceFile(path: \"{page.SourcePath}\")", page.Description, page.Keywords));
        }

        foreach (var file in BmageSourceCatalog.SourceFiles)
        {
            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetBmageSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty));
        }

        return [.. entries];
    }
}
