using Bit.Bmotion.Demo.Server.Dtos;
using Bit.Bmotion.Demo.Client;
using Bit.Utilities.Mcp;

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
/// enough to know what to ask for next. The ranking itself is <see cref="McpSearch"/>, shared with
/// the other utilities' MCP servers: it reads a query word by its stem ("fading" finds "fade"),
/// only from the start of a word ("item" is not inside "TItem", nor "fade" inside "Crossfade"), and
/// by how rare it is, so the words a request is phrased in ("list", "item") cannot outrank the one it
/// is about ("fade").
/// </para>
/// </summary>
public static class BmotionSearchIndex
{
    /// <summary>
    /// The words that separate nothing here. Every entry is about animating something in Bmotion, so
    /// a query saying so matches everything and ranks nothing; and the words a problem is reported in
    /// outrank the answer when they are left in: "why does my animation not run" scores every RunAsync
    /// method and every NotFound file in the corpus. They are dropped when the query says something
    /// else as well, and a query made of nothing else is not searched at all.
    /// </summary>
    private static readonly string[] _ambientWords =
    [
        "bmotion", "animate", "animation", "animated", "animating", "blazor", "element",
        "not", "run", "runs", "running", "work", "works", "working", "use", "using",
        "can", "will", "wont", "isnt", "doesnt", "instead", "still", "just", "only", "even"
    ];

    /// <summary>
    /// What a request calls the one element it animates: "fade a list item in" and "slide the card in" ask for what
    /// fades or slides one element in, not for the staggered list or the reorder group's Items, which "list" and
    /// "item" would otherwise name. Said of one thing they are dropped; "items", "each list item" and "one item at a
    /// time" are about the items of a list, and stay questions about the list.
    /// </summary>
    private static readonly string[] _ambientNouns = ["item", "list item", "card", "row", "tile"];

    // Not readonly: a Lazy<Task> holds on to a faulted task, which would answer every later search
    // with the exception the first one hit. Replacing it lets the next search build the index again.
    // Built asynchronously because two of the catalogs it indexes run the engine to describe themselves.
    private static Lazy<Task<McpSearch>> _search = new(BuildAsync);

    /// <summary>The best matches for a query, each with the call that returns it in full.</summary>
    public static async Task<BmotionSearchHitDto[]> SearchAsync(string? query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var result = (await GetSearchAsync()).Search(query, limit);

        return [.. result.Hits.Select(hit => new BmotionSearchHitDto
        {
            Kind = hit.Entry.Kind,
            Title = hit.Entry.Title,
            Context = hit.Entry.Context,
            Tool = hit.Entry.Tool,
            Snippet = hit.Snippet
        })];
    }

    private static async Task<McpSearch> GetSearchAsync()
    {
        var lazy = _search;

        try
        {
            return await lazy.Value;
        }
        catch (Exception)
        {
            // Whoever loses this race has the winner's fresh Lazy, which is equally unbuilt.
            Interlocked.CompareExchange(ref _search, new Lazy<Task<McpSearch>>(BuildAsync), lazy);

            throw;
        }
    }

    private static async Task<McpSearch> BuildAsync()
    {
        McpSearchEntry[] entries = [.. await BuildEntriesAsync()];

        return new McpSearch(() => entries, _ambientWords, searchAmbientAlone: false, ambientNouns: _ambientNouns, preferExactNames: true);
    }

    private static async Task<List<McpSearchEntry>> BuildEntriesAsync()
    {
        var entries = new List<McpSearchEntry>(768);

        foreach (var section in BmotionSourceCatalog.GuideSections)
        {
            var body = BmotionSourceCatalog.GetGuideSection(section.Heading) ?? string.Empty;

            entries.Add(new McpSearchEntry("Guide section", section.Heading, section.Parent,
                $"GetBmotionGuideSection(heading: \"{section.Heading}\")", body, string.Empty));
        }

        // The setup guides carry the render-mode capability matrices, which is where the answer to
        // "it works locally but not in production" lives. Without them indexed, a question phrased
        // as a symptom finds whatever else happens to contain the word "server".
        foreach (var renderMode in BmotionSetupGuide.RenderModes)
        {
            var guide = BmotionSetupGuide.Get(renderMode) ?? string.Empty;

            entries.Add(new McpSearchEntry("Setup guide", $"Setup for {renderMode}", "Render mode",
                $"GetBmotionSetupGuide(renderMode: \"{renderMode}\")", guide,
                $"{renderMode} render mode install register services DI container degrade capability"));
        }

        // Two entries for the tools that answer by running the engine. They are the right response
        // to a whole class of question - "will this work on Server", "how long does this spring
        // take", "why does nothing move" - that names no type, section or recipe, and so would
        // otherwise match nothing at all.
        entries.Add(new McpSearchEntry("Tool", "Check whether an animation works on Blazor Server", "Runs the real engine",
            "AnalyzeBmotionAnimation(properties: \"...\", transition: \"...\")",
            "Reports where the engine plays an animation: in the browser as a Web Animation - a two-keyframe " +
            "timeline, or a recording of the frames the engine draws - which plays everywhere including Blazor " +
            "Server, or only on the C# per-frame loop, which exists only on WebAssembly and collapses to an instant " +
            "state change on Server. Explains why, what it costs, and what to change.",
            "server compositor degrade snap snaps instant instantly jump render mode waapi web animation timeline recorded recording " +
            "keyframes cost frame loop production interactive auto"));

        entries.Add(new McpSearchEntry("Tool", "Measure what a transition feels like", "Runs the real engine",
            "SimulateBmotionTransition(transition: \"...\")",
            "Plays a spring, tween or inertia transition off-screen and reports its settle time, overshoot, wobble " +
            "count and curve shape. A spring has no duration argument, so this is the only way to know how long " +
            "one takes or how far it overshoots before shipping it.",
            "spring settle duration overshoot bounce wobble slow fast feel feels tune timing damping stiffness"));

        foreach (var recipe in BmotionRecipeCatalog.All)
        {
            entries.Add(new McpSearchEntry("Recipe", recipe.Title, "Copy-pasteable pattern",
                $"GetBmotionRecipe(id: \"{recipe.Id}\")",
                $"{recipe.Intent}\n{recipe.Notes}", $"{recipe.Id} {recipe.Keywords}"));
        }

        foreach (var type in BmotionApiCatalog.Types)
        {
            entries.Add(new McpSearchEntry($"API {type.Kind.ToLowerInvariant()}", type.Name, null,
                $"GetBmotionApiDetails(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = BmotionApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: the overloads of a method are one thing to
            // know about, and all are read by the same call anyway. Their documentation is searched as
            // one body, so a term in any overload finds the member - each sentence once, however many
            // overloads repeat it.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();
                var kind = $"API {member.Kind.ToLowerInvariant()}";
                var body = string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim()).Where(text => text.Length > 0).Distinct(StringComparer.Ordinal));
                var signatures = string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim()).Where(text => text.Length > 0).Distinct(StringComparer.Ordinal));

                // An inherited member - ChildStagger on every transition type, say - is one answer, not four:
                // left in, the copies push everything else off the page. They stay indexed on every type, so
                // the copy a query names its type for ("BmSpring ChildStagger") is the one it finds. Keyed by
                // the types that declare it, not by what its documentation says: two members of one name that
                // share a stock sentence ("Style" on two components) are still two answers.
                string[] declaredBy = [.. overloads.Select(o => o.DeclaringType).OfType<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
                var sameAs = declaredBy.Length > 0 ? $"{member.Kind}:{string.Join('+', declaredBy)}.{member.Name}" : null;

                // Named by the member alone: "TItem" of "BmotionReorderGroup<TItem>.Items" and the words of
                // the type's own name say where the member lives, which every other member of the type shares.
                entries.Add(new McpSearchEntry(kind, $"{type.Name}.{member.Name}", type.Name,
                    $"GetBmotionApiDetails(typeName: \"{type.Name}\")", body, signatures,
                    Name: member.Name, SameAs: sameAs));
            }
        }

        foreach (var property in await BmotionPropertyCatalog.GetAsync())
        {
            entries.Add(new McpSearchEntry("Animatable property", property.Name, property.Category,
                "GetBmotionAnimatableProperties()",
                $"{property.Notes} Writes {property.Css}. Played by the browser as: " +
                $"{property.BrowserPlayback?.ToLowerInvariant() ?? "nothing - the frame loop only"}. " +
                $"On Blazor Server: {property.OnBlazorServer}.".Trim(),
                $"{property.Css} {property.Example}"));
        }

        foreach (var easing in await BmotionEasingCatalog.GetAsync())
        {
            // Named by the preset alone, as a member is: "EaseInOut" is the preset's name, and "BmEase" the
            // type every preset shares.
            entries.Add(new McpSearchEntry("Easing preset", $"BmEase.{easing.Name}", easing.Family,
                "GetBmotionEasings()", easing.Feel, $"{easing.Direction} {easing.Family}", Name: easing.Name));
        }

        // The demo pages and their sources are examples, not answers: a page is the whole of a
        // feature, so it mentions every word a question about any part of it is phrased in.
        foreach (var page in DocsCatalog.AllPages)
        {
            entries.Add(new McpSearchEntry("Demo page", page.Title, "Live example",
                $"GetBmotionSourceFile(path: \"{BmotionSourceCatalog.SourcePathOf(page)}\")", page.Description, page.Keywords,
                McpSearch.ExampleWeight));
        }

        foreach (var file in BmotionSourceCatalog.SourceFiles)
        {
            entries.Add(new McpSearchEntry("Source file", file.Path, file.Kind,
                $"GetBmotionSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty,
                McpSearch.ExampleWeight));
        }

        return entries;
    }
}
