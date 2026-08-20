using System.Text;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Brouter.Demo.Server.Dtos;
using Bit.Brouter.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Brouter.Demo.Server.Controllers;

/// <summary>
/// The Brouter MCP server: the tools an AI agent calls to build features with Bit.Brouter without
/// guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Brouter, the README, the docs pages rendered by the very router
/// they describe, and the demo's source files - so an agent gets what the current version actually
/// does rather than a snapshot someone wrote down. The same methods are exposed as plain HTTP GET
/// endpoints under /api/mcp/..., which makes each of them inspectable from a browser.
/// </para>
/// <para>
/// There are eight of them, and one rule holds across the reference ones: a tool takes the key of
/// the thing you want and answers with that thing; leave the key out and it answers with the index
/// of what there is. That is what a listing tool used to be - and a listing tool costs a name, a
/// description and a schema in every request an agent makes, forever, to say what an optional
/// argument says for nothing.
/// </para>
/// <para>
/// Each tool carries the two annotations that mean something for a tool which only reads:
/// <c>ReadOnly</c>, which is what lets a client run it without stopping to ask a person, and
/// <c>OpenWorld = false</c>, which says the answers come from a closed, known body of material
/// rather than from the internet. The protocol's other two hints - destructive and idempotent - are
/// defined only for tools that do modify something, so stating them here would say nothing.
/// </para>
/// <para>
/// Only the two tools whose answer is machine-actionable - a ranked hit carrying its own follow-up
/// call, a parse verdict - set <c>UseStructuredContent</c> and publish an output schema. The rest
/// answer in Markdown, and deliberately: a structured answer goes over the wire twice, once as the
/// object and once as the JSON text the spec asks for on its behalf, and the same reference read as
/// Markdown is smaller than either half. A client pays that price where an object is worth having;
/// it should not pay it to read documentation.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController(HtmlRenderer htmlRenderer, IOptions<BrouterOptions> brouterOptions) : ControllerBase
{
    // The docs pages are rich enough that a couple of them would otherwise dominate a client's
    // context window; the ones in this demo land far below the cap.
    private const int MaxDocumentLength = 40_000;

    // A route table pasted in by an agent is a handful of lines. A file pasted in by mistake is
    // thousands, and every one of them costs a parse - so the tool answers about the beginning of
    // it, rather than spending the request on material nobody meant to send.
    private const int MaxAnalyzedTemplates = 200;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBrouter), Title = "Search everything about Bit.Brouter", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Brouter at once - the reference guide, the documentation pages, every public type and member, the route constraints and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section, page or type holds the answer, then call the hit's tool verbatim. Example queries: 'block navigation unsaved changes', 'cache loader data', 'keep component alive', 'query string binding'.")]
    public BrouterSearchResultDto SearchBrouter(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BrouterSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterSetupGuide), Title = "Setup guide for one render mode", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Brouter to a Blazor app in one render mode, as the real files of a working project: 'server' (Blazor Web App, InteractiveServer), 'wasm' (InteractiveWebAssembly), 'auto' (InteractiveAuto) or 'standalone-wasm'. Call this before writing any setup code - which DI container registers the services and where the catch-all host page lives differ per render mode. It stands in for several shorter answers: do not also fetch the getting-started page for the same task.")]
    public string GetBrouterSetupGuide(
        // Declared rather than merely described: the four values then sit in the JSON schema itself,
        // where a model cannot pass a fifth past them, and a client offers them as completions
        // instead of asking a person to remember how this server spells "standalone-wasm".
        [AllowedValues("server", "wasm", "auto", "standalone-wasm")]
        [Description("The app's Blazor render mode.")] string renderMode)
    {
        return BrouterSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BrouterSetupGuide.RenderModes)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Brouter reference guide (the library's README) as Markdown, with its code samples - e.g. 'Async guards', 'Data loader', 'Keep-alive routes', 'Typed routes (source generator)'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes. This is the finest-grained prose there is: for one feature it costs a fraction of the documentation page covering the same ground.")]
    public string GetBrouterGuideSection(
        [Description("The section's heading, e.g. 'Async guards'. Case and punctuation are ignored. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BrouterSourceCatalog.RenderGuideIndex();

        var section = BrouterSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BrouterSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Heading}'")
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBrouterGuideSection with no heading for the index of sections.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterDocsPage), Title = "Read a documentation page", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one page of the Bit.Brouter documentation site as Markdown, code samples included - the narrative behind the guide, and the only place some of the material exists at all: 'faq' (a symptom mapped to its cause), 'recipes' (worked solutions), 'navigation-pipeline' (what runs when, in order), 'migration', 'performance'. The other slugs: overview, getting-started, route-templates, constraints, route-parameters, nested-routes, page-discovery, navigation, guards, scroll-and-focus, data-loading, view-transitions, lifecycle, typed-routes, api, mcp. Omit the slug for the index of pages.")]
    public async Task<string> GetBrouterDocsPage(
        [Description("The page's slug, e.g. 'guards'. Pass 'overview' for the documentation overview; omit it for the index of pages.")] string? slug = null)
    {
        if (slug is null) return DocsPageRenderer.RenderIndex();

        var page = DocsPageRenderer.FindPage(slug);

        if (page is null) return DocsPageRenderer.NoSuchPage(slug);

        // The page is rendered by the same component the site serves, so the documentation an agent
        // reads is the documentation a human reads - there is no second copy that could go stale.
        // The renderer holds on to what it produced, so only the first caller pays for a render.
        var (rendered, error) = await DocsPageRenderer.TryRenderMarkdownAsync(htmlRenderer, page);

        if (rendered is null) return DocsPageRenderer.Unavailable(page, error);

        // The page renders its own <h1>, so only its source is prepended here.
        return Truncate($"Bit.Brouter documentation page: {page.Url}\n\n{rendered}");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Brouter as Markdown, read out of the shipped assembly: one type with every Blazor parameter, property, method, event or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'Broute', 'Brouter', 'BrouterLink', 'IBrouter', 'BrouterOptions', 'BrouterNavigationContext'.")]
    public string GetBrouterApi(
        [Description("The type's name without its namespace, e.g. 'Broute' or 'BrouterOptions'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BrouterApiCatalog.RenderIndex();

        var rendered = BrouterApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BrouterApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Brouter has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Brouter has no public type called '{typeName}'. Call GetBrouterApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterRouteConstraints), Title = "List the route constraints", ReadOnly = true, OpenWorld = false)]
    [Description("Lists every route constraint usable inside a Bit.Brouter route template - the built-in type and validation constraints, a custom one, and constraint chaining - each with the rule it enforces plus a passing and a failing example, as one Markdown table.")]
    public string GetBrouterRouteConstraints()
    {
        return BrouterConstraintReference.Render();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBrouterRouteTemplates), Title = "Inspect route templates", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Parses route templates with Bit.Brouter's own parser and reports what they mean. One template comes back in full: its segments, parameter names, constraints, default values, specificity, and notes about behavior that is easy to get wrong. Several - one per line - come back as a ranked set instead: the order the router prefers them in when more than one matches a URL, the ones that are indistinguishable (Brouter throws at registration for those), and the exact error for each invalid one. Check a template with it before you ship it. At most 200 templates are analyzed in one call; send more and the answer comes back with isPartial set, covering only the first 200.")]
    public BrouterRouteAnalysisDto InspectBrouterRouteTemplates(
        [Description("One route template, written exactly as it would be in @page or <Broute Path=\"...\">, e.g. '/users/{id:int}' - or several, one per line. A semicolon separates them too, and so does a top-level comma, but never a comma inside a constraint such as range(1,10).")] string templates)
    {
        var analyzed = SplitTemplates(templates, out var submitted);

        // Nothing but whitespace came in, and the empty template is a real one: it is what a child
        // route declares to be its parent's index. So the answer is about that template rather than
        // an empty list, which would read as "there is nothing to say about this" - and the note it
        // carries also tells a caller who simply sent an empty argument what to send instead.
        if (analyzed.Length == 0) analyzed = [string.Empty];

        // The app's own constraint registry, so custom constraints registered in
        // AddBitBrouterServices (this demo registers "slug") resolve exactly as they do at runtime.
        var analysis = BrouterTemplateInspector.Analyze(analyzed, brouterOptions.Value.Constraints);

        if (submitted <= analyzed.Length) return analysis;

        // An analysis of part of a route table is not an analysis of the route table: the ambiguity
        // report above all is only ever as complete as the set it saw. So the answer says which it
        // is, in its own shape and not only in prose, and says it before anything else it has to
        // say - an agent that stopped reading here still cannot mistake it for a clean table.
        return analysis with
        {
            IsPartial = true,
            SubmittedTemplateCount = submitted,
            AnalyzedTemplateCount = analyzed.Length,
            Notes =
            [
                $"INCOMPLETE ANALYSIS: {submitted} templates were sent and only the first {analyzed.Length} were " +
                $"analyzed, which is all this server analyzes in one call. The rest were not parsed and were not " +
                $"compared against these for ambiguity, so this says nothing about them - send them in a further call.",
                .. analysis.Notes
            ]
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Brouter source file verbatim - e.g. 'Demo/Client/AppRouter.razor', the complete route table of this documentation site, which exercises nearly every feature. Omit the path for the index of files: the demo's route table and the pages it routes to, and the minimal hosting samples for each Blazor render mode ('Sample/Wasm/...', 'Sample/Server/...', 'Sample/Auto/...'). These are worked examples - for what a parameter means, GetBrouterApi answers in a fraction of the characters.")]
    public string GetBrouterSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/AppRouter.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return BrouterSourceCatalog.RenderSourceIndex();

        var content = BrouterSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BrouterSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBrouterSourceFile with no path for the index of files.";
        }

        return Truncate(content);
    }

    /// <summary>
    /// Splits a pasted route table into templates.
    /// <para>
    /// A newline or a semicolon separates two templates. A comma does not, however much it looks
    /// like a list separator: it is part of the grammar being analyzed - <c>{id:range(1,10)}</c>,
    /// <c>{code:length(2,4)}</c> - and splitting on it would tear one valid template into two
    /// invalid ones, then report the wreckage as the caller's mistake. So a comma separates only
    /// where it sits outside every brace and parenthesis, which is where a hand-written list of
    /// templates puts it anyway.
    /// </para>
    /// </summary>
    internal static string[] SplitTemplates(string? templates) => SplitTemplates(templates, out _);

    /// <inheritdoc cref="SplitTemplates(string?)"/>
    /// <param name="templates">The route table as it was written.</param>
    /// <param name="submitted">
    /// How many templates it held, before the cap - which is more than the return value holds
    /// whenever the caller sent more than this server analyzes at once, and the only way the answer
    /// can own up to being partial instead of passing a cut table off as the whole one.
    /// </param>
    internal static string[] SplitTemplates(string? templates, out int submitted)
    {
        // Only the first MaxAnalyzedTemplates are kept - a pasted file of any size costs this list
        // nothing past the cap - while every template found still counts towards `submitted`, which
        // is what lets the answer own up to how much of the table it left out.
        var kept = new List<string>();
        var count = 0;
        var current = new StringBuilder();
        var depth = 0;

        foreach (var c in templates ?? string.Empty)
        {
            if (c is '{' or '(') depth++;
            else if (c is '}' or ')') depth = Math.Max(0, depth - 1);

            if (c is '\n' or '\r' or ';' || (c is ',' && depth == 0))
            {
                Flush(kept, ref count, current);

                continue;
            }

            current.Append(c);
        }

        Flush(kept, ref count, current);

        submitted = count;

        return [.. kept];

        static void Flush(List<string> kept, ref int count, StringBuilder current)
        {
            var part = current.ToString().Trim();

            current.Clear();

            if (part.Length == 0) return;

            count++;

            if (kept.Count < MaxAnalyzedTemplates) kept.Add(part);
        }
    }

    internal static string Truncate(string text)
    {
        if (text.Length <= MaxDocumentLength) return text;

        // Never between the two halves of a surrogate pair: half a pair is not text, and a client
        // that re-encodes the answer turns it into a replacement character or rejects it outright.
        var cut = char.IsHighSurrogate(text[MaxDocumentLength - 1]) ? MaxDocumentLength - 1 : MaxDocumentLength;

        return $"{text[..cut]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }
}
