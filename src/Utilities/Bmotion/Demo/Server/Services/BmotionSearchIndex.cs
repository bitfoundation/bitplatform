using System.Text;
using Bit.Bmotion.Demo.Server.Dtos;
using Bit.Bmotion.Demo.Client.Shared;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the guide, every public type and
/// member, the animatable properties, the easing presets, the recipes, the demo pages and their
/// sources.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there.
/// "How do I make a list appear one item at a time?" is a guide section (Orchestration), a
/// component parameter (Transition.ChildStagger), a recipe (staggered-list) and a demo page
/// (/variants) all at once, and none of those contain the word the question was asked with. Each
/// hit therefore carries the exact follow-up call that returns its full text, so one search is
/// enough to know what to ask for next.
/// </para>
/// </summary>
public static class BmotionSearchIndex
{
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted)
    {
        /// <summary>
        /// The title split into words, camel-case humps included, so "StaggerChildren" is found by
        /// "stagger" - and so a query word only counts as a title hit when it IS one of those words,
        /// rather than merely appearing inside one.
        /// </summary>
        public string[] TitleWords { get; } = SplitWords(Title);

        /// <summary>
        /// The name without the type that owns it: "StaggerChildren" out of
        /// "BmTransition.StaggerChildren". Splitting the title into humps alone would not match a
        /// query typed as the whole name - which is how a member is written in code, and therefore
        /// how it is searched for.
        /// </summary>
        public string LocalName { get; } = Title[(Title.LastIndexOf('.') + 1)..];
    }

    private const int MaxTerms = 16;

    // Not readonly: a Lazy<Task> holds on to a faulted task, which would answer every later search
    // with the exception the first one hit. Replacing it lets the next search build the index again.
    private static Lazy<Task<Entry[]>> _entries = new(BuildAsync);

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto",
        // Every entry in this index is about animating something in Bmotion; a query saying so
        // matches everything and ranks nothing.
        "bmotion", "animate", "animation", "animated", "animating", "blazor", "element",
        // The words a problem is reported in. Left in, they outrank the answer: "why does my
        // animation not run" scores every RunAsync method and every NotFound file in the corpus.
        "not", "why", "run", "runs", "running", "work", "works", "working", "use", "using",
        "can", "will", "wont", "isnt", "doesnt", "instead", "still", "just", "only", "even"
    };

    /// <summary>The best matches for a query, each with the call that returns it in full.</summary>
    public static async Task<BmotionSearchHitDto[]> SearchAsync(string query, int limit)
    {
        var terms = Tokenize(query);
        if (terms.Length == 0) return [];

        var entries = await GetEntriesAsync();

        return [.. entries
            .Select(entry => (Entry: entry, Score: Score(entry, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(hit => new BmotionSearchHitDto
            {
                Kind = hit.Entry.Kind,
                Title = hit.Entry.Title,
                Context = hit.Entry.Context,
                Tool = hit.Entry.Tool,
                Snippet = Snippet(hit.Entry.Body, terms)
            })];
    }

    private static async Task<Entry[]> GetEntriesAsync()
    {
        var lazy = _entries;

        try
        {
            return await lazy.Value;
        }
        catch (Exception)
        {
            // Whoever loses this race has the winner's fresh Lazy, which is equally unbuilt.
            Interlocked.CompareExchange(ref _entries, new Lazy<Task<Entry[]>>(BuildAsync), lazy);

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
            // for "StaggerChildren" wants the parameter, not the paragraphs that mention it.
            score += (isTitleWord ? 12 : 0) + inTitle * 3 + inBoosted * 5 + Math.Min(inBody, 6);
        }

        // Every term matching is the strongest signal a hit is the right one.
        return matched == 0 ? 0 : score * matched;
    }

    /// <summary>
    /// Same word, plural aside - "spring" has to find the section called "Springs", and nobody
    /// phrases a question in the number the heading happens to use.
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
        var current = new StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) is false)
            {
                if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
                continue;
            }

            // A capital after a lowercase letter starts a new word ("StaggerChildren" -> stagger
            // children), while a run of capitals stays together ("SVG", "CSS").
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
            // One- and two-letter words match everything and rank nothing, and the words a question
            // is phrased with do worse than nothing.
            .Where(term => term.Length > 2 && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            // Every term is counted in every entry's body, so the work is terms x corpus. No question
            // is phrased in more words than this, while a pasted file as a query would scan for ages.
            .Take(MaxTerms)];
    }

    private static async Task<Entry[]> BuildAsync()
    {
        var entries = new List<Entry>(768);

        foreach (var section in BmotionSourceCatalog.GuideSections)
        {
            var body = BmotionSourceCatalog.GetGuideSection(section.Heading) ?? string.Empty;

            entries.Add(new Entry("Guide section", section.Heading, section.Parent,
                $"GetBmotionGuideSection(heading: \"{section.Heading}\")", body, string.Empty));
        }

        // The setup guides carry the render-mode capability matrices, which is where the answer to
        // "it works locally but not in production" lives. Without them indexed, a question phrased
        // as a symptom finds whatever else happens to contain the word "server".
        foreach (var renderMode in BmotionSetupGuide.RenderModes)
        {
            var guide = BmotionSetupGuide.Get(renderMode) ?? string.Empty;

            entries.Add(new Entry("Setup guide", $"Setup for {renderMode}", "Render mode",
                $"GetBmotionSetupGuide(renderMode: \"{renderMode}\")", guide,
                $"{renderMode} render mode install register services DI container degrade capability"));
        }

        // Two entries for the tools that answer by running the engine. They are the right response
        // to a whole class of question - "will this work on Server", "how long does this spring
        // take", "why does nothing move" - that names no type, section or recipe, and so would
        // otherwise match nothing at all.
        entries.Add(new Entry("Tool", "Check whether an animation works on Blazor Server", "Runs the real engine",
            "AnalyzeBmotionAnimation(properties: \"...\", transition: \"...\")",
            "Reports which playback path the engine chose for an animation: the browser compositor, which plays " +
            "everywhere including Blazor Server, or the C# per-frame loop, which exists only on WebAssembly and " +
            "collapses to an instant state change on Server. Explains why, and what to change.",
            "server compositor degrade snap instant jump render mode waapi frame loop production interactive auto"));

        entries.Add(new Entry("Tool", "Measure what a transition feels like", "Runs the real engine",
            "SimulateBmotionTransition(transition: \"...\")",
            "Plays a spring, tween or inertia transition off-screen and reports its settle time, overshoot, wobble " +
            "count and curve shape. A spring has no duration argument, so this is the only way to know how long " +
            "one takes or how far it overshoots before shipping it.",
            "spring settle duration overshoot bounce wobble too slow too fast feel tune timing damping stiffness"));

        foreach (var recipe in BmotionRecipeCatalog.All)
        {
            entries.Add(new Entry("Recipe", recipe.Title, "Copy-pasteable pattern",
                $"GetBmotionRecipe(id: \"{recipe.Id}\")",
                $"{recipe.Intent}\n{recipe.Notes}", $"{recipe.Id} {recipe.Keywords}"));
        }

        foreach (var type in BmotionApiCatalog.Types)
        {
            entries.Add(new Entry($"API {type.Kind.ToLowerInvariant()}", type.Name, null,
                $"GetBmotionApiDetails(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = BmotionApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            foreach (var member in details.Members)
            {
                entries.Add(new Entry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBmotionApiDetails(typeName: \"{type.Name}\")",
                    $"{member.Summary} {member.Remarks}".Trim(),
                    $"{member.Type} {member.Signature}".Trim()));
            }
        }

        foreach (var property in await BmotionPropertyCatalog.GetAsync())
        {
            entries.Add(new Entry("Animatable property", property.Name, property.Category,
                "GetBmotionAnimatableProperties()",
                $"{property.Notes} Writes {property.Css}. On Blazor Server: {property.OnBlazorServer}.".Trim(),
                $"{property.Css} {property.Example}"));
        }

        foreach (var easing in await BmotionEasingCatalog.GetAsync())
        {
            entries.Add(new Entry("Easing preset", $"BmEase.{easing.Name}", easing.Family,
                "GetBmotionEasings()", easing.Feel, $"{easing.Direction} {easing.Family}"));
        }

        foreach (var page in NavItem.All)
        {
            entries.Add(new Entry("Demo page", page.Title, "Live example",
                $"GetBmotionSourceFile(path: \"{page.SourcePath}\")", page.Description, page.Keywords));
        }

        foreach (var file in BmotionSourceCatalog.SourceFiles)
        {
            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetBmotionSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty));
        }

        return [.. entries];
    }
}
