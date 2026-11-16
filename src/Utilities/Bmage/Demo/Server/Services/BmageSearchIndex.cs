using Bit.Bmage.Demo.Client;
using Bit.Bmage.Demo.Server.Dtos;
using Bit.Utilities.Mcp;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: every section of every guide, the
/// setup guides, every public type and member of the package, the recipes, the review rules,
/// the demo pages and their sources.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there.
/// "Make an avatar under 100 KB" is a recipe (avatar-crop), a guide section (Quality and size
/// budgets), an options property (ImageExportOptions.MaxFileSize), a demo page (/avatar) and a tool
/// that measures it (RunBmagePipeline) all at once. Each hit therefore carries the exact follow-up
/// call that returns its full text, so one search is enough to know what to ask for next. The
/// ranking itself is <see cref="McpSearch"/>, shared with the other utilities' MCP servers: it reads
/// a query word by its stem ("uploaded" finds "upload"), only ever as a whole word ("under" is not
/// inside "underlying"), and by how rare it is, so the words a request is phrased in ("without",
/// "photo") cannot outrank the one it is about ("shrink").
/// </para>
/// </summary>
public static class BmageSearchIndex
{
    /// <summary>
    /// The words that separate nothing here. Every entry is about images in Bmage, so a query saying so
    /// matches everything and ranks nothing; and the words a problem is reported in ("why does it NOT
    /// WORK", "can I USE") outrank the answer when they are left in. They are dropped only when the query
    /// says something else as well.
    /// </summary>
    private static readonly string[] _ambientWords =
    [
        "bmage", "bit", "blazor", "image", "images", "picture", "pictures",
        "not", "use", "using", "can", "will", "wont", "isnt", "doesnt", "instead", "still", "just", "only", "even",
        "work", "works", "working",
    ];

    private static readonly McpSearch _search = new(Build, _ambientWords);

    /// <summary>The best matches for a query, each with the call that returns it in full.</summary>
    public static BmageSearchHitDto[] Search(string? query, int limit)
    {
        var result = _search.Search(query, limit);

        // A query made of nothing but those words ("bmage image") is searched by them as a last resort,
        // which here ranks every entry on the strength of a word all of them share: no answer is better.
        if (result.Terms.All(term => _ambientWords.Contains(term, StringComparer.OrdinalIgnoreCase))) return [];

        return [.. result.Hits.Select(hit => new BmageSearchHitDto
        {
            Kind = hit.Entry.Kind,
            Title = hit.Entry.Title,
            Context = hit.Entry.Context,
            Tool = hit.Entry.Tool,
            Snippet = hit.Snippet
        })];
    }

    private static IEnumerable<McpSearchEntry> Build()
    {
        foreach (var document in BmageSourceCatalog.Documents)
        {
            yield return new McpSearchEntry("Guide", document.Title, document.Name,
                $"GetBmageDocument(name: \"{document.Name}\")", document.Summary ?? string.Empty,
                $"{document.Name} {string.Join(' ', document.Sections)}");

            foreach (var (heading, level) in BmageSourceCatalog.GetHeadings(document.Name))
            {
                var body = BmageSourceCatalog.GetDocumentSection(document.Name, heading) ?? string.Empty;

                yield return new McpSearchEntry("Guide section", heading, $"{document.Title}{(level == 3 ? " (sub-section)" : null)}",
                    $"GetBmageDocument(name: \"{document.Name}\", section: \"{heading}\")", body, string.Empty);
            }
        }

        // The setup guides carry the per-host facts - which registration, which containers, which
        // codecs - where the answer to "works in development, fails at prerender" lives. Prerendering
        // is left to their prose, which says it only for the hosts that prerender: named on all six,
        // it filled the page for every question about it with six copies of one answer.
        foreach (var mode in BmageSetupGuide.RenderModes)
        {
            yield return new McpSearchEntry("Setup guide", $"Setup for {mode}", "Hosting model",
                $"GetBmageSetupGuide(renderMode: \"{mode}\")", BmageSetupGuide.Get(mode) ?? string.Empty,
                $"{mode} install register services addbitbmageservices addbitbmageheadlessservices container host program");
        }

        // The tools that answer by running the engine. They are the right response to a class of
        // question - "how big will this be", "is this budget reachable", "does Preserve keep GPS" -
        // that names no type, section or recipe, and so would otherwise match nothing at all.
        yield return new McpSearchEntry("Tool", "Measure a pipeline: output size, bytes per format and quality, timings", "Runs the real engine",
            "RunBmagePipeline(pipeline: \"...\")",
            "Runs a pipeline on a generated photo, screenshot or graphic with the managed codecs and reports the output size, " +
            "the encoded bytes for each format and quality, a MaxFileSize search, the optimised operation list and the timings.",
            "bytes size kb file quality compression jpeg png budget maxfilesize compare formats slow fast timing performance optimiser optimizer measure");

        yield return new McpSearchEntry("Tool", "Check what each metadata policy keeps (GPS, serial number, owner)", "Runs the real engine",
            "CompareBmageMetadataPolicies(format: \"jpeg\")",
            "Encodes a GPS-tagged camera photo under Remove, StripSensitive and Preserve with the real encoder and reads each file back.",
            "gps location privacy exif metadata strip remove preserve stripsensitive leak serial owner geotag");

        yield return new McpSearchEntry("Tool", "Review Bmage code for runtime mistakes", "Checks code",
            "ReviewBmageCode(code: \"...\")",
            "Finds the mistakes that compile cleanly: browser calls during prerendering, undisposed buffers, missing registrations, " +
            "missing stylesheet, AVIF without a fallback or on a server, Preserve leaking GPS.",
            "review check lint audit bug mistake wrong error");

        foreach (var recipe in BmageRecipeCatalog.All)
        {
            yield return new McpSearchEntry("Recipe", recipe.Title, $"Copy-pasteable pattern ({recipe.RunsIn})",
                $"GetBmageRecipe(id: \"{recipe.Id}\")",
                $"{recipe.Intent}\n{recipe.Notes}", $"{recipe.Id} {recipe.Keywords}");
        }

        foreach (var rule in BmageCodeReview.Catalog)
        {
            yield return new McpSearchEntry("Review rule", rule.Id, $"{rule.Severity}, from {rule.Source}",
                "ReviewBmageCode(code: \"...\")", rule.Catches, rule.Id.Replace('-', ' '));
        }

        // An inherited member documented once - TryFuse on every adjustment, say - is one answer, not
        // eight: indexed on every type that inherits it, the copies push everything else off the page.
        var inherited = new HashSet<(string Kind, string Name, string Body)>();

        foreach (var type in BmageApiCatalog.Types)
        {
            yield return new McpSearchEntry($"API {type.Kind.ToLowerInvariant()}", type.Name, $"{type.Namespace} ({type.Package})",
                $"GetBmageApiDetails(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty);

            var details = BmageApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: the overloads of a method are one thing to
            // know about, and all are read by the same call anyway. Their documentation is searched as
            // one body, so a term in any overload finds the member.
            foreach (var overloads in details.Members.Where(member => member.Kind != "Constructor").GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();
                var kind = $"API {member.Kind.ToLowerInvariant()}";
                var body = string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim();

                if (inherited.Add((kind, member.Name, body)) is false) continue;

                yield return new McpSearchEntry(kind, $"{type.Name}.{member.Name}", type.Name,
                    $"GetBmageApiDetails(typeName: \"{type.Name}.{member.Name}\")", body,
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim(),
                    Name: member.Name);
            }
        }

        // The demo pages and their sources are examples, not answers: a page is the whole of a
        // feature, so it mentions every word a question about any part of it is phrased in.
        foreach (var page in DocsCatalog.AllPages)
        {
            yield return new McpSearchEntry("Demo page", page.Title, "Live example",
                $"GetBmageSourceFile(path: \"{BmageSourceCatalog.SourcePathOf(page)}\")", page.Description, page.Keywords, McpSearch.ExampleWeight);
        }

        // The site's own shell is not an example of anything an agent would write with Bit.Bmage;
        // a search for "palette" or "header" should not hand it back as one.
        foreach (var file in BmageSourceCatalog.SourceFiles.Where(file => file.Kind != BmageSourceCatalog.SiteChromeKind))
        {
            yield return new McpSearchEntry("Source file", file.Path, file.Kind,
                $"GetBmageSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, McpSearch.ExampleWeight);
        }
    }
}
