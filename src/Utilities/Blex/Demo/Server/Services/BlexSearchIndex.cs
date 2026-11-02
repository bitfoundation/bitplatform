using Bit.Blex.Demo.Server.Dtos;
using Bit.Utilities.Mcp;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, the docs pages,
/// every public type and member, the generator's diagnostics and the demo's source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there - "how do
/// I cancel a running request?" is a guide section, a docs page, an attribute property and a demo store
/// all at once. Each hit therefore carries the exact follow-up tool call that returns the full text, so
/// one search is enough to know what to ask for next. The ranking itself is <see cref="McpSearch"/>.
/// </para>
/// </summary>
public static class BlexSearchIndex
{
    /// <summary>
    /// The words a caller from another state library - or one who only says what the app has to do - uses
    /// for a Blex concept. They are matched as extra vocabulary of the entries that answer them, so
    /// "reducer" finds actions, "selector" finds the subscription APIs and a value that "survives a reload"
    /// finds persistence.
    /// </summary>
    private static readonly Dictionary<string, string> _synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Actions"] = "reducer dispatch mutation mutate creator createaction typed strict",
        ["Effects"] = "async side effect thunk saga fetch http loading",
        ["Computed state"] = "derived getter memoized memo selector",
        ["Components & selectors"] = "rerender re-render subscribe subscription selector usestore useselector observer observable rx",
        ["Scoped stores"] = "instance componentstore component-scoped provider context multiple dialog tab",
        ["Persistence"] = "localstorage sessionstorage save reload survive rehydrate hydrate",
        ["Undo & redo"] = "history undo redo",
        ["DevTools & time travel"] = "redux devtools timetravel debugger dispatcher",
        ["Testing"] = "test unit deterministic fake clock timeprovider replay",
        ["Entity adapter"] = "normalized collection crud list",
        ["Middleware"] = "interceptor logger logging veto",
    };

    /// <summary>"state" and "management" are what Blex is, so every entry here carries them.</summary>
    private static readonly McpSearch _search = new(Build, ambientWords: ["blex", "bit", "blazor", "state", "management"]);

    public static BlexSearchResultDto Search(string? query, int limit)
    {
        var result = _search.Search(query, limit);

        if (result.Terms.Length == 0)
        {
            return new BlexSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held nothing but filler words and words too short to search by, so there was " +
                          "nothing to rank by. Search for the thing itself - 'effect cancel', 'persist', 'selector' - " +
                          "rather than for a sentence."
            };
        }

        if (result.Hits.Length > 0)
        {
            return new BlexSearchResultDto
            {
                Hits = [.. result.Hits.Select(hit => new BlexSearchHitDto
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

        return new BlexSearchResultDto
        {
            Terms = words,
            Hits = [],
            Message = result.DidYouMean.Length > 0
                ? $"Nothing matched {string.Join(" + ", words)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", words)}. Try one word rather than several, or call " +
                   "GetBlexGuideSection, GetBlexDocsPage or GetBlexApi with no argument at all for the index of what " +
                   "there is. A concept Blex does not name is often the same idea under another word - a 'reducer' is an " +
                   "action, a 'thunk' is an effect, a 'getter' is a computed value.",
            DidYouMean = result.DidYouMean.Length > 0 ? result.DidYouMean : null
        };
    }

    private static IEnumerable<McpSearchEntry> Build()
    {
        foreach (var section in BlexSourceCatalog.GuideSections)
        {
            var body = BlexSourceCatalog.GetGuideSection(section.Heading) ?? string.Empty;

            yield return new McpSearchEntry("Guide section", section.Heading, section.Parent,
                $"GetBlexGuideSection(heading: \"{section.Heading}\")", body, _synonyms.GetValueOrDefault(section.Heading) ?? string.Empty);
        }

        foreach (var page in DocsPageRenderer.Pages)
        {
            yield return new McpSearchEntry("Docs page", page.Title, page.Section,
                $"GetBlexDocsPage(slug: \"{page.Slug}\")", page.Summary, _synonyms.GetValueOrDefault(page.Title) ?? string.Empty);
        }

        foreach (var type in BlexApiCatalog.Types)
        {
            yield return new McpSearchEntry($"API {type.Kind.ToLowerInvariant()}", type.Name, type.Package,
                $"GetBlexApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty);

            var details = BlexApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: two overloads are one thing to know about, and
            // both are read by the same call anyway.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();

                yield return new McpSearchEntry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBlexApi(typeName: \"{type.Name}\")",
                    string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim(),
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim(),
                    Name: member.Name);
            }
        }

        foreach (var diagnostic in BlexDiagnosticsReference.Entries)
        {
            yield return new McpSearchEntry("Diagnostic", $"{diagnostic.Id} {diagnostic.Title}", diagnostic.Severity,
                $"GetBlexDiagnostics(code: \"{diagnostic.Id}\")",
                $"{diagnostic.MessageFormat} {diagnostic.Doc?.Cause}".Trim(),
                $"{diagnostic.Id} build error compile generator");
        }

        foreach (var file in BlexSourceCatalog.SourceFiles)
        {
            yield return new McpSearchEntry("Source file", file.Path, file.Kind,
                $"GetBlexSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, McpSearch.ExampleWeight);
        }
    }
}
