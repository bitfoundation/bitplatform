using Bit.BlazeDb.Demo.Server.Dtos;
using Bit.Utilities.Mcp;

namespace Bit.BlazeDb.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, every public
/// type and member, the generator's diagnostics, the setup guides, the table inspector and the demo's
/// source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there -
/// "keep the todos after a reload" is a guide section, a storage backend, its factory method, a
/// setup guide and a demo page all at once. Each hit therefore carries the exact follow-up tool call
/// that returns the full text, so one search is enough to know what to ask for next. The ranking
/// itself is <see cref="McpSearch"/>.
/// </para>
/// </summary>
public static class BlazeDbSearchIndex
{
    /// <summary>
    /// The README sections that are about the README or the site rather than about the library: the demo's
    /// page list names every topic and so matches every query, and the one about this MCP server lists
    /// tools that mention setup, tables and diagnostics in a paragraph that answers none of them.
    /// </summary>
    private static readonly HashSet<string> _metaSections = new(StringComparer.OrdinalIgnoreCase) { "The demo", "MCP server" };

    /// <summary>
    /// The words a caller uses for what a guide section is about without the section using them: someone
    /// who wants to "encrypt the database" or "add a property" is asking for "Encryption at rest" and
    /// "Schema evolution", in words neither heading contains in a form the stemming can reach. They count
    /// as the section's name (<see cref="McpSearchEntry.Aliases"/>), so they rank it where its heading would.
    /// </summary>
    private static readonly Dictionary<string, string> _synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Not "key": a name, it would answer every question about a table's key with this section.
        ["Encryption at rest"] = "encrypt encryption cipher passphrase password secret aes",
        ["Schema evolution"] = "add remove rename reorder property column migration migrate field number version upgrade",
        ["Multiple tabs"] = "tab replica lock writer broadcast",
        ["Storage quota"] = "quota space full disk eviction evicted persist",
        ["Persistence guarantees"] = "survive reload persist persistent durable durability lost lose crash",
        ["Threading model"] = "thread concurrent concurrency parallel race",
        ["Streaming results"] = "stream large batch freeze frozen yield responsive",
        ["Hosting models"] = "prerender hosting render mode",
        ["Rows are live objects"] = "mutate mutation modify modified copy instance",
        ["Without OPFS"] = "firefox safari private fallback indexeddb",
        ["Files on the desktop"] = "file directory disk desktop console",
        ["How LINQ is translated"] = "linq where orderby query translate translation",
        ["Opening the store"] = "ensurecreated ensurecreatedasync ensuredeleted seed",
    };

    /// <summary>"blazedb" and "blazor database library" are what a caller reaches for first, and made of nothing but these.</summary>
    private static readonly McpSearch _search = new(Build, ambientWords: ["blazedb", "bit", "blazor", "library", "database", "db"]);

    public static BlazeDbSearchResultDto Search(string? query, int limit)
    {
        var result = _search.Search(query, limit);

        if (result.Terms.Length == 0)
        {
            // Nothing was left to search by, which is a different failure from "searched and found
            // nothing" - and the fix is on the caller's side, so it has to be said rather than
            // returned as an empty list the agent would read as "BlazeDb cannot do this".
            return new BlazeDbSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held nothing but filler words and words too short to search by, so there was " +
                          "nothing to rank by. Search for the thing itself - 'opfs', 'transaction', 'unique index' - " +
                          "rather than for a sentence."
            };
        }

        if (result.Hits.Length > 0)
        {
            // No echo of the query and no list of the terms it was reduced to: the caller sent that
            // query one message ago, and what it was tokenized into changes nothing about a hit it
            // can read for itself. Both are worth saying only when there is nothing else to say.
            return new BlazeDbSearchResultDto
            {
                Hits = [.. result.Hits.Select(hit => new BlazeDbSearchHitDto
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

        return new BlazeDbSearchResultDto
        {
            Terms = words,
            Hits = [],
            Message = result.DidYouMean.Length > 0
                ? $"Nothing matched {string.Join(" + ", words)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", words)}. Try one word rather than several, or call " +
                   "GetBlazeDbGuideSection or GetBlazeDbApi with no argument at all for the index of what " +
                   "there is. A concept BlazeDb does not name is often the same idea under another word - " +
                   "'persist' or 'offline' is storage (OPFS, IndexedDB, files), a 'column' is a property and its field " +
                   "number, a 'SELECT' or 'WHERE' is a lookup, a range or a BlazeDbQuery, a 'migration' is schema evolution, " +
                   "'LINQ' is the EF Core provider, 'commit' is a transaction.",
            DidYouMean = result.DidYouMean.Length > 0 ? result.DidYouMean : null
        };
    }

    private static IEnumerable<McpSearchEntry> Build()
    {
        foreach (var section in BlazeDbSourceCatalog.GuideSections)
        {
            // The sections about the README and the site rather than the library stay readable through
            // GetBlazeDbGuideSection; they are only kept out of the ranking.
            if (_metaSections.Contains(section.Key)) continue;

            var body = BlazeDbSourceCatalog.GetGuideSection(section.Key) ?? string.Empty;

            yield return new McpSearchEntry("Guide section", section.Heading, section.Parent,
                $"GetBlazeDbGuideSection(heading: \"{section.Key}\")", body, string.Empty, Aliases: _synonyms.GetValueOrDefault(section.Heading));
        }

        foreach (var type in BlazeDbApiCatalog.Types)
        {
            yield return new McpSearchEntry($"API {type.Kind.ToLowerInvariant()}", type.Name, type.Package,
                $"GetBlazeDbApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty);

            var details = BlazeDbApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: two overloads of UseBlazeDb are two members
            // and one thing to know about, and both are read by the same call anyway. Their
            // documentation is searched as one body, so a term in either overload finds the member.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();

                yield return new McpSearchEntry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBlazeDbApi(typeName: \"{type.Name}\")",
                    string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim(),
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim(),
                    Name: member.Name);
            }
        }

        // The table as a whole, for the question that asks for the list itself ("which errors can the
        // generator report") rather than for one of its rows.
        yield return new McpSearchEntry("Diagnostic", "Generator diagnostics", null,
            "GetBlazeDbDiagnostics()", BlazeDbDiagnosticsReference.RenderIndex(), "diagnostic error warning generator build compile");

        foreach (var diagnostic in BlazeDbDiagnosticsReference.Entries)
        {
            yield return new McpSearchEntry("Diagnostic", $"{diagnostic.Id} {diagnostic.Title}", diagnostic.Severity,
                $"GetBlazeDbDiagnostics(code: \"{diagnostic.Id}\")",
                $"{diagnostic.MessageFormat} {diagnostic.Doc?.Cause}".Trim(),
                $"{diagnostic.Id} build error compile generator");
        }

        foreach (var model in BlazeDbSetupGuide.HostingModels)
        {
            yield return new McpSearchEntry("Setup guide", $"Setup: {model}", null,
                $"GetBlazeDbSetupGuide(hostingModel: \"{model}\")", BlazeDbSetupGuide.Summary(model),
                $"setup install wiring register hosting {model.Replace('-', ' ')}");
        }

        yield return new McpSearchEntry("Table inspector", "Inspect a table type", null,
            "InspectBlazeDbTable(source: \"...\")",
            "Runs the real Bit.BlazeDb source generator over C# source containing [BlazeDbTable] types and reports its BLZ " +
            "diagnostics, the compiler errors, the table name, the key, the field number and wire type of every property, " +
            "the hash, ordered and compound indexes and the generated Table and Indexes members.",
            "inspect validate check verify table schema model generator field number wire type");

        foreach (var file in BlazeDbSourceCatalog.SourceFiles)
        {
            yield return new McpSearchEntry("Source file", file.Path, file.Kind,
                $"GetBlazeDbSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, McpSearch.ExampleWeight);
        }
    }
}
