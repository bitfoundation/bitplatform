using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Blex.Demo.Server.Dtos;
using Bit.Blex.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blex.Demo.Server.Controllers;

/// <summary>
/// The Blex MCP server: the tools an AI agent calls to build with Bit.Blex without guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML documentation
/// compiled into the Bit.Blex assemblies, the README, the docs pages rendered by the very components the
/// site serves, the source generator itself and the demo's source files - so an agent gets what the
/// current version actually does rather than a snapshot someone wrote down. The same methods are exposed
/// as plain HTTP GET endpoints under /api/mcp/..., which makes each of them inspectable from a browser.
/// </para>
/// <para>
/// One rule holds across the reference tools: a tool takes the key of the thing you want and answers
/// with that thing; leave the key out and it answers with the index of what there is.
/// </para>
/// <para>
/// Every tool is <c>ReadOnly</c> - which lets a client run it without stopping to ask a person - and
/// <c>OpenWorld = false</c>: the answers come from a closed, known body of material. Only the two tools
/// whose answer is machine-actionable - a ranked hit carrying its own follow-up call, the generator's
/// verdict on a store - publish an output schema; the rest answer in Markdown, which costs a model a
/// fraction of the same reference sent as JSON.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController(HtmlRenderer htmlRenderer) : ControllerBase
{
    // The docs pages are rich enough that a couple of them would otherwise dominate a client's context
    // window; the ones in this demo land below the cap.
    private const int MaxDocumentLength = 40_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBlex), Title = "Search everything about Bit.Blex", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Blex at once - the reference guide, the documentation pages, every public type and member, the source generator's diagnostics and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section, page or type holds the answer, then call the hit's tool verbatim. Example queries: 'cancel effect', 'persist store localStorage', 'selector re-render', 'undo'.")]
    public BlexSearchResultDto SearchBlex(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BlexSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlexSetupGuide), Title = "Setup guide for one hosting model", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Blex to an app, as the real files of a working project where there is one: 'server' (Blazor Web App, InteractiveServer), 'wasm' (InteractiveWebAssembly with prerendering), 'auto' (InteractiveAuto), 'standalone-wasm', 'maui-hybrid' (MAUI Blazor Hybrid) or 'maui' (native XAML). Call this before writing any setup code - which DI container registers the stores, with which lifetime, and where <BlexProvider> goes differ per hosting model.")]
    public string GetBlexSetupGuide(
        [AllowedValues("server", "wasm", "auto", "standalone-wasm", "maui-hybrid", "maui")]
        [Description("The app's hosting model, e.g. 'wasm' for a Blazor Web App rendering InteractiveWebAssembly.")] string hostingModel)
    {
        return BlexSetupGuide.Get(hostingModel)
            ?? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", BlexSetupGuide.HostingModels)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlexGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Blex reference guide (the library's README) as Markdown, with its code samples - e.g. 'The whole store', 'Cancellation and concurrency', 'Persistence', 'Middleware: observe and veto'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes. This is the finest-grained prose there is: for one feature it costs a fraction of the documentation page covering the same ground.")]
    public string GetBlexGuideSection(
        [Description("The section's heading, e.g. 'Persistence'. Case and punctuation are ignored. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BlexSourceCatalog.RenderGuideIndex();

        var section = BlexSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BlexSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Heading}'")
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBlexGuideSection with no heading for the index of sections.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlexDocsPage), Title = "Read a documentation page", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one page of the Bit.Blex documentation site as Markdown, code samples included - the narrative behind the guide: 'stores', 'computed', 'actions', 'effects', 'subscriptions', 'cross-store', 'entities', 'middleware', 'errors', 'persistence', 'history', 'devtools', 'testing', 'diagnostics', 'api', plus 'introduction', 'installation', 'quick-start' and 'mcp'. Omit the slug for the index of pages.")]
    public async Task<string> GetBlexDocsPage(
        [Description("The page's slug, e.g. 'effects'. Omit it for the index of pages.")] string? slug = null)
    {
        if (string.IsNullOrWhiteSpace(slug)) return DocsPageRenderer.RenderIndex();

        var page = DocsPageRenderer.FindPage(slug);

        if (page is null) return DocsPageRenderer.NoSuchPage(slug);

        // Rendered by the same component the site serves, so the documentation an agent reads is the
        // documentation a human reads. Only the first caller pays for the render.
        var (rendered, error) = await DocsPageRenderer.TryRenderMarkdownAsync(htmlRenderer, page);

        if (rendered is null) return DocsPageRenderer.Unavailable(page, error);

        return Truncate($"Bit.Blex documentation page: {page.Url}\n\n{rendered}");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlexApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Blex as Markdown, read out of the shipped assemblies (Bit.Blex, Bit.Blex.Blazor, Bit.Blex.Maui, Bit.Blex.Testing): one type with every parameter, property, method, event or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'BlexStoreBase', 'BlexOptions', 'BitBlex', 'BlexEffectAttribute', 'BlexProvider', 'BlexEntityAdapter'.")]
    public string GetBlexApi(
        [Description("The type's name without its namespace, e.g. 'BlexOptions' or 'BlexEntityAdapter'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BlexApiCatalog.RenderIndex();

        var rendered = BlexApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BlexApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Blex has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Blex has no public type called '{typeName}'. Call GetBlexApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlexDiagnostics), Title = "Explain a generator diagnostic", ReadOnly = true, OpenWorld = false)]
    [Description("Explains one BLEX diagnostic of the Bit.Blex source generator - its severity, the message a build reports, why the generator rejects the shape, a rejected store and the fixed one. Omit the code for the table of every diagnostic. Call it whenever a build or InspectBlexStore reports a BLEX id.")]
    public string GetBlexDiagnostics(
        [Description("The diagnostic id, e.g. 'BLEX001' ('blex1' and '1' work too). Omit it for the table of every diagnostic.")] string? code = null)
    {
        if (string.IsNullOrWhiteSpace(code)) return BlexDiagnosticsReference.RenderIndex();

        var entry = BlexDiagnosticsReference.Find(code);

        return entry is not null
            ? BlexDiagnosticsReference.Render(entry)
            : $"The Blex generator has no diagnostic '{code}'. It reports {string.Join(", ", BlexDiagnosticsReference.Entries.Select(e => e.Id))}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBlexStore), Title = "Inspect a store with the real generator", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs the real Bit.Blex source generator over C# source containing one or more [BlexStore] classes and reports its verdict: every BLEX diagnostic a build would report, the C# compiler errors in the result, and the exact public members the generator adds to each store (generated property names, action and effect wrappers, Cancel methods, loading/error properties). Check every store with it before you build on it, and call the generated names it lists rather than guessed ones. The usual usings (System, System.Threading.Tasks, Bit.Blex, ...) are implied, so a class on its own is enough.")]
    public BlexStoreInspectionDto InspectBlexStore(
        [Description("The store source: one or more classes marked [BlexStore], as they would be written in a .cs file. At most 50,000 characters.")] string source,
        [Description("True to also return the full generated source. Off by default - the member list is what a caller normally needs.")] bool includeGeneratedCode = false)
    {
        return BlexStoreInspector.Inspect(source, includeGeneratedCode);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlexSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Blex source file verbatim - e.g. 'Demo/Client/Stores/CartStore.cs' (a store with state, computed values and actions) or 'Demo/Client/Extensions/IServiceCollectionExtensions.cs' (the complete registration of this documentation site). Omit the path for the index of files: the demo's stores, middleware and components, and the minimal standalone WebAssembly sample ('Sample/...'). These are worked examples - for what a member means, GetBlexApi answers in a fraction of the characters.")]
    public string GetBlexSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Stores/CartStore.cs'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return BlexSourceCatalog.RenderSourceIndex();

        var content = BlexSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BlexSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBlexSourceFile with no path for the index of files.";
        }

        return Truncate(content);
    }

    internal static string Truncate(string text)
    {
        if (text.Length <= MaxDocumentLength) return text;

        // Never between the two halves of a surrogate pair: half a pair is not text.
        var cut = char.IsHighSurrogate(text[MaxDocumentLength - 1]) ? MaxDocumentLength - 1 : MaxDocumentLength;

        return $"{text[..cut]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }
}
