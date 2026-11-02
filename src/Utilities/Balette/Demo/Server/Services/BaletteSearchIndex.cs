using Bit.Balette.Demo.Server.Dtos;
using Bit.Utilities.Mcp;

namespace Bit.Balette.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, every public
/// type and member, the setup guides, the stylesheet's variables, classes and hooks, the search lab
/// and the demo's source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there -
/// "a rename command that asks for the new name" is a guide section, a result type, a builder method
/// and a demo page all at once. Each hit therefore carries the exact follow-up tool call that returns
/// the full text, so one search is enough to know what to ask for next. The ranking itself is
/// <see cref="McpSearch"/>.
/// </para>
/// </summary>
public static class BaletteSearchIndex
{
    /// <summary>
    /// The words a custom property's name abbreviates, so a question asked in words finds it: "active row background"
    /// finds <c>--bit-balette-active-bg</c>, "muted text colour" <c>--bit-balette-fg-muted</c>.
    /// </summary>
    private static string SpelledOut(string variable) => string.Join(' ', variable.Split('-', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part switch
        {
            "bg" => "background",
            "fg" => "foreground text colour color",
            "kbd" => "keyboard key cap",
            "z" => "z-index stacking layer",
            "mono" => "monospace font",
            "gap" => "spacing padding",
            "radius" => "corner rounded",
            "soft" => "tint",
            _ => null,
        })
        .Where(word => word is not null));

    /// <summary>
    /// The words a caller uses for something Balette calls otherwise, and the word Balette uses. A query
    /// word finds what the library's word finds as well as what it finds itself: a command that "asks for"
    /// a name is one whose handler returns a prompt, and "hotkey" finds every entry about shortcuts.
    /// <para>
    /// Only a word that means nothing else here belongs in this table: "input" is the search box as much
    /// as a prompt's text field, "filter" the matcher as much as a mode, and "step" anything that comes
    /// in order, so they are searched for as themselves.
    /// </para>
    /// </summary>
    private static readonly (string Word, string Means)[] _synonyms =
    [
        ("ask", "prompt"),
        ("hotkey", "shortcut"),
        ("keybinding", "shortcut"),
        ("alias", "keyword"),
        ("favorite", "pin"),
        ("favourite", "pin"),
        ("submenu", "nested"),
        ("remote", "async"),
    ];

    /// <summary>
    /// Two sections of the README are about the README rather than about the library: the table of contents,
    /// which names every topic and so matches every query, and the one about this MCP server, whose list of
    /// tools mentions search, setup, themes and the API in a paragraph that answers none of them.
    /// </summary>
    private static readonly HashSet<string> _metaSections = new(StringComparer.OrdinalIgnoreCase) { "Documentation", "MCP server" };

    /// <summary>"balette" and "blazor command palette" are what a caller reaches for first, and made of almost nothing but these.</summary>
    private static readonly McpSearch _search = new(Build, ambientWords: ["balette", "bit", "blazor", "library", "palette"], _synonyms);

    public static BaletteSearchResultDto Search(string? query, int limit)
    {
        var result = _search.Search(query, limit);

        if (result.Terms.Length == 0)
        {
            // Nothing was left to search by, which is a different failure from "searched and found
            // nothing" - and the fix is on the caller's side, so it has to be said rather than
            // returned as an empty list the agent would read as "Balette cannot do this".
            return new BaletteSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held nothing but filler words and words too short to search by, so there was " +
                          "nothing to rank by. Search for the thing itself - 'prompt', 'shortcut', 'async source' - " +
                          "rather than for a sentence."
            };
        }

        if (result.Hits.Length > 0)
        {
            // No echo of the query and no list of the terms it was reduced to: the caller sent that
            // query one message ago, and what it was tokenized into changes nothing about a hit it
            // can read for itself. Both are worth saying only when there is nothing else to say.
            return new BaletteSearchResultDto
            {
                Hits = [.. result.Hits.Select(hit => new BaletteSearchHitDto
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

        return new BaletteSearchResultDto
        {
            Terms = words,
            Hits = [],
            Message = result.DidYouMean.Length > 0
                ? $"Nothing matched {string.Join(" + ", words)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", words)}. Try one word rather than several, or call " +
                   "GetBaletteGuideSection or GetBaletteApi with no argument at all for the index of what " +
                   "there is. A concept Balette does not name is often the same idea under another word - a " +
                   "'command menu', 'quick pick' or 'spotlight' is the palette, a 'submenu' or 'step' is a nested page, " +
                   "'input' or 'text box' is a prompt, 'scope' or 'filter' is a mode, 'remote results' are an async " +
                   "source, 'hotkey' is a shortcut, 'aliases' are keywords, 'favorites' are pins.",
            DidYouMean = result.DidYouMean.Length > 0 ? result.DidYouMean : null
        };
    }

    private static IEnumerable<McpSearchEntry> Build()
    {
        foreach (var section in BaletteSourceCatalog.GuideSections)
        {
            // The sections about the README stay readable through GetBaletteGuideSection; they are only
            // kept out of the ranking.
            if (_metaSections.Contains(section.Key)) continue;

            var body = BaletteSourceCatalog.GetGuideSection(section.Key) ?? string.Empty;

            yield return new McpSearchEntry("Guide section", section.Heading, section.Parent,
                $"GetBaletteGuideSection(heading: \"{section.Key}\")", body, string.Empty);
        }

        foreach (var type in BaletteApiCatalog.Types)
        {
            yield return new McpSearchEntry($"API {type.Kind.ToLowerInvariant()}", type.Name, null,
                $"GetBaletteApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty);

            var details = BaletteApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: the overloads of Handler are several members
            // and one thing to know about, and all are read by the same call anyway. Their
            // documentation is searched as one body, so a term in any overload finds the member.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();

                yield return new McpSearchEntry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBaletteApi(typeName: \"{type.Name}\")",
                    string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim(),
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim(),
                    Name: member.Name);
            }
        }

        foreach (var mode in BaletteSetupGuide.RenderModes)
        {
            yield return new McpSearchEntry("Setup guide", $"Setup: {mode}", null,
                $"GetBaletteSetupGuide(renderMode: \"{mode}\")", BaletteSetupGuide.Summary(mode),
                $"setup install wiring register services stylesheet render mode {mode.Replace('-', ' ')}");
        }

        yield return new McpSearchEntry("Search lab", "Inspect a search", null,
            "InspectBaletteSearch(query: \"rc\")",
            "Runs the library's own matcher and ranker over a query and a list of commands - this demo's registry by " +
            "default - and reports, per command, whether it matched and why not, its score, its rank and its row, and " +
            "the characters it highlights.",
            "highlight match fuzzy operators negate exact prefix suffix frecency priority order",
            // What it is for: a caller asking why a command ranks where it does is asking for this, whatever else
            // the question names.
            Aliases: "debug explain rank score");

        // The table as a whole, for the question that asks for the list itself ("theme variables",
        // "dark mode colours") rather than for one of its rows.
        yield return new McpSearchEntry("Theme variable", "Theme variable catalog", null,
            "GetBaletteThemeVariables()", BaletteThemeCatalog.Render(), "css theme variables colors colours dark light mode restyle unstyled");

        foreach (var variable in BaletteThemeCatalog.Variables)
        {
            // A row is one property and its values. The rows share one parameterless call, so
            // however many of them match, a search spends one hit on them.
            var dark = variable.DarkValue is null ? null : $" (dark: {variable.DarkValue})";

            yield return new McpSearchEntry("Theme variable", variable.Name, variable.Group,
                "GetBaletteThemeVariables()", $"{variable.Name}: {variable.LightValue}{dark}", $"css {variable.Group} {SpelledOut(variable.Name)}");
        }

        foreach (var hook in BaletteThemeCatalog.Hooks)
        {
            yield return new McpSearchEntry("Markup hook", $"data-balette=\"{hook.Value}\"", hook.Component,
                "GetBaletteThemeVariables()",
                $"The <{hook.Element}> {hook.Component} renders{(hook.Class is null ? null : $" with class {hook.Class}")}{(hook.States.Length == 0 ? null : $", carrying {string.Join(", ", hook.States)}")}.",
                "unstyled data attribute hook test selector");
        }

        foreach (var rule in BaletteStylesheetClasses.Classes)
        {
            // The classes share one file, so however many of them match, a search spends one hit on them.
            yield return new McpSearchEntry("CSS class", rule.Selector, "bit-balette.css",
                $"GetBaletteSourceFile(path: \"{BaletteSourceCatalog.StylesheetPath}\")", rule.Description, "css class style stylesheet");
        }

        foreach (var file in BaletteSourceCatalog.SourceFiles)
        {
            yield return new McpSearchEntry("Source file", file.Path, file.Kind,
                $"GetBaletteSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, McpSearch.ExampleWeight);
        }
    }
}
