using Bit.Bonboarding.Demo.Server.Dtos;
using Bit.Utilities.Mcp;

namespace Bit.Bonboarding.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, every public
/// type and member, the theme variables and the demo's source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there -
/// "advance when the user clicks the button" is a guide section, a step option, a trigger type and a
/// demo page all at once. Each hit therefore carries the exact follow-up tool call that returns the
/// full text, so one search is enough to know what to ask for next. The ranking itself is
/// <see cref="McpSearch"/>.
/// </para>
/// </summary>
public static class BonboardingSearchIndex
{
    /// <summary>
    /// The words a caller uses for what a guide section is about without the section using them: a step
    /// that "waits for a panel that loads later" is asking for the missing-target strategies and how
    /// waiting works, in words neither section contains. They count as the section's name
    /// (<see cref="McpSearchEntry.Aliases"/>), so they rank it where its heading would - above
    /// <c>RemindMeLater</c>, which only shares the word "later".
    /// </summary>
    private static readonly Dictionary<string, string> _synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["3. Start it"] = "code programmatic programmatically",
        ["Missing-target strategies"] = "absent appear later late load lazy slow delayed yet",
        ["How waiting works"] = "appear later late load lazy slow delayed yet",
        ["Multi-page tours"] = "several multiple across navigate navigation",
    };

    /// <summary>
    /// What a guide section is called, where that is less than its heading (<see cref="McpSearchEntry.Name"/>): a
    /// heading word that only says how. As the name of "Lazy loading on WebAssembly", "loading" would make the section
    /// the answer to "load" alone - which asks about a page load, and is answered by "Surviving a full page load". In
    /// the heading it still counts, so "lazy load" still finds the section first.
    /// </summary>
    private static readonly Dictionary<string, string> _names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Lazy loading on WebAssembly"] = "Lazy WebAssembly",
    };

    /// <summary>"bonboarding" and "blazor onboarding tour" are what a caller reaches for first, and made of nothing but these.</summary>
    private static readonly McpSearch _search = new(Build, ambientWords: ["bonboarding", "bit", "blazor", "onboarding", "tour", "tours"]);

    public static BonboardingSearchResultDto Search(string? query, int limit)
    {
        var result = _search.Search(query, limit);

        if (result.Terms.Length == 0)
        {
            // Nothing was left to search by, which is a different failure from "searched and found
            // nothing" - and the fix is on the caller's side, so it has to be said rather than
            // returned as an empty list the agent would read as "Bonboarding cannot do this".
            return new BonboardingSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held nothing but filler words and words too short to search by, so there was " +
                          "nothing to rank by. Search for the thing itself - 'spotlight', 'branching', 'missing target' - " +
                          "rather than for a sentence."
            };
        }

        if (result.Hits.Length > 0)
        {
            // No echo of the query and no list of the terms it was reduced to: the caller sent that
            // query one message ago, and what it was tokenized into changes nothing about a hit it
            // can read for itself. Both are worth saying only when there is nothing else to say.
            return new BonboardingSearchResultDto
            {
                Hits = [.. result.Hits.Select(hit => new BonboardingSearchHitDto
                {
                    Kind = hit.Entry.Kind,
                    Title = hit.Entry.Title,
                    Context = hit.Entry.Context,
                    Tool = hit.Entry.Tool,
                    Snippet = hit.Snippet
                })],
                HasMore = result.HasMore
            };
        }

        var words = result.Terms;

        return new BonboardingSearchResultDto
        {
            Terms = words,
            Hits = [],
            Message = result.DidYouMean.Length > 0
                ? $"Nothing matched {string.Join(" + ", words)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", words)}. Try one word rather than several, or call " +
                   "GetBonboardingGuideSection or GetBonboardingApi with no argument at all for the index of what " +
                   "there is. A concept Bonboarding does not name is often the same idea under another word - a " +
                   "'tooltip' or 'hint' is a single-step tour, a 'walkthrough' is a tour, 'highlight' is the spotlight, " +
                   "'wizard step validation' is CanAdvance.",
            DidYouMean = result.DidYouMean.Length > 0 ? result.DidYouMean : null
        };
    }

    private static IEnumerable<McpSearchEntry> Build()
    {
        foreach (var section in BonboardingSourceCatalog.GuideSections)
        {
            var body = BonboardingSourceCatalog.GetGuideSection(section.Heading) ?? string.Empty;

            yield return new McpSearchEntry("Guide section", section.Heading, section.Parent,
                $"GetBonboardingGuideSection(heading: \"{section.Heading}\")", body, string.Empty,
                Name: _names.GetValueOrDefault(section.Heading), Aliases: _synonyms.GetValueOrDefault(section.Heading));
        }

        foreach (var type in BonboardingApiCatalog.Types)
        {
            yield return new McpSearchEntry($"API {type.Kind.ToLowerInvariant()}", type.Name, null,
                $"GetBonboardingApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty);

            var details = BonboardingApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: two overloads of StartAsync are two members
            // and one thing to know about, and both are read by the same call anyway. Their
            // documentation is searched as one body, so a term in either overload finds the member.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();

                yield return new McpSearchEntry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBonboardingApi(typeName: \"{type.Name}\")",
                    string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim(),
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim(),
                    Name: member.Name);
            }
        }

        // The table as a whole, for the question that asks for the list itself ("theme variables",
        // "dark mode colours") rather than for one of its rows.
        yield return new McpSearchEntry("Theme variable", "Theme variable catalog", null,
            "GetBonboardingThemeVariables()", BonboardingThemeCatalog.Render(), "css theme variables colors colours dark mode restyle");

        foreach (var variable in BonboardingThemeCatalog.Variables)
        {
            // A row is one property and its values. The rows share one parameterless call, so
            // however many of them match, a search spends one hit on them.
            var dark = variable.DarkValue is null ? null : $" (dark: {variable.DarkValue})";

            yield return new McpSearchEntry("Theme variable", variable.Name, variable.Group,
                "GetBonboardingThemeVariables()", $"{variable.Name}: {variable.LightValue}{dark}", $"css {variable.Group}");
        }

        foreach (var file in BonboardingSourceCatalog.SourceFiles)
        {
            yield return new McpSearchEntry("Source file", file.Path, file.Kind,
                $"GetBonboardingSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, McpSearch.ExampleWeight);
        }
    }
}
