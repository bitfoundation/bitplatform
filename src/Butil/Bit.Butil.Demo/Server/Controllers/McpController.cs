using System.ComponentModel;
using Microsoft.AspNetCore.Cors;
using ModelContextProtocol.Server;
using Bit.Butil.Demo.Client.Docs;
using Bit.Butil.Demo.Server.Dtos;
using Bit.Butil.Demo.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Butil.Demo.Server.Controllers;

/// <summary>
/// The Butil MCP server: the tools an AI agent calls to reach the browser platform from Blazor
/// without guessing at the API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Butil, the README, the docs pages rendered by the very
/// components they describe, and the demo's source files - so an agent gets what the current
/// version actually does rather than a snapshot someone wrote down. The same methods are exposed as
/// plain HTTP GET endpoints under /api/mcp/..., which makes each of them inspectable from a browser.
/// </para>
/// <para>
/// There are seven of them, and the count is the design rather than what was left over. A tool's
/// description is paid for in every request of every session a client has this server connected,
/// and it is paid for again in the model's attention every time it chooses between two tools that
/// sound alike. So a listing is not a tool - it is what a retrieval tool answers when it is asked
/// for nothing in particular; a single-item lookup is not a tool when a tool that takes a set
/// already resolves each member of it; and nothing here restates what the server's own
/// <c>instructions</c> have already put in the model's context before the first call.
/// </para>
/// <para>
/// Every one of them carries the same four annotations, because every one of them is the same kind
/// of call: it reads, it reads only from this process, and asking twice gives the same answer.
/// Those are not decoration. A client that is told a tool is read-only can run it without stopping
/// to ask a person for confirmation first, which is the difference between an agent that consults
/// the documentation and one that guesses rather than interrupt; and OpenWorld = false says the
/// answers come from this build rather than from the web, so a disagreement with a search result is
/// this library's version of the truth.
/// </para>
/// <para>
/// None of them publishes an output schema. The three that answer with data used to, and the schema
/// is not what it cost: a tool declared with UseStructuredContent answers with the object in
/// structuredContent AND the same JSON, byte for byte, in a text block, because the protocol asks a
/// server to keep answering clients that cannot read a schema. So every search, every reference and
/// every plan crossed the wire twice, and the schemas themselves were a third of what tools/list
/// costs a session before a single call is made. What a client gets now is the same JSON, once.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
// Fully qualified: Microsoft.AspNetCore.Components brings its own RouteAttribute, and this file
// needs that namespace for the renderer and the NavigationManager.
[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
// On the controller rather than on MapControllers(): the open policy belongs to the GET mirror of
// the MCP tools, which is public read-only documentation, and nothing else. A controller added to
// this app later would otherwise inherit it by having been mapped alongside this one.
[EnableCors(McpController.CorsPolicy)]
public class McpController(HtmlRenderer htmlRenderer, NavigationManager navigationManager, IHttpContextAccessor httpContextAccessor,
                          ILogger<McpController> logger) : ControllerBase
{
    /// <summary>The CORS policy this controller and the /mcp endpoint share. Defined in Program.cs.</summary>
    public const string CorsPolicy = "mcp";

    [HttpGet]
    [McpServerTool(Name = nameof(SearchButil), Title = "Search everything about Bit.Butil",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Searches everything known about Bit.Butil at once - the reference guide, the documentation pages, every public type and member, the browser-support matrix and the demo's source files - and returns the best matches, each with the exact follow-up tool call that returns its full text. Use this first whenever you do not already know which service or member does the job. Example queries: 'copy text to clipboard', 'keep the screen awake', 'detect when an element scrolls into view', 'store data offline', 'read a file the user picked'.")]
    public ButilSearchResultDto SearchButil(string query, int limit = 12)
    {
        var hits = ButilSearchIndex.Search(query, limit);

        if (hits.Length > 0) return new ButilSearchResultDto { Hits = hits };

        // An empty list on its own is unreadable: every other tool here answers an input it cannot
        // resolve with a sentence naming what to try instead, and this is the one agents call first.
        return new ButilSearchResultDto
        {
            Hits = [],
            Message = ButilSearchIndex.IsSearchable(query)
                ? $"Nothing in Bit.Butil matches '{query}'. Try the capability rather than the wording - " +
                  "\"copy text\", \"screen awake\", \"scrolls into view\" - or call GetButilDocsPage with no " +
                  "slug for every documented API and what it needs."
                : $"'{query}' carries no searchable term: words under three letters and words common to " +
                  "every entry here (\"how\", \"the\", \"get\", \"browser\", \"blazor\", \"butil\") are dropped " +
                  "before matching. Search for the capability itself, e.g. \"clipboard\" or \"wake lock\"."
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilSetupGuide), Title = "Setup guide for one hosting model",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Butil to a Blazor app in one hosting model, as the real files of a working project: 'wasm' (standalone Blazor WebAssembly), 'web-app' (Blazor Web App with prerendering), 'server' (Blazor Server) or 'hybrid' (MAUI/WPF/WinForms). Call this before writing any setup code - which host page carries the script tag and how many DI containers register the services differ per hosting model, and getting either wrong produces an app where every browser call silently does nothing.")]
    public string GetButilSetupGuide(string hostingModel)
    {
        var guide = ButilSetupGuide.Get(hostingModel);

        // Truncated like every other document this controller hands out: this is the one that
        // concatenates several whole files, so it is the last one that should go out uncapped.
        return guide is null
            ? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", ButilSetupGuide.HostingModels)}."
            : Truncate(guide);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilApiDetails), Title = "Full reference of one type",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the full reference of one Bit.Butil type: every method with its complete signature and default parameter values, every property, event or enum value, each with the XML documentation that ships with the library. Call it before using a member you are unsure about, e.g. 'Clipboard', 'LocalStorage', 'Geolocation', 'ElementReferenceExtensions', 'ButilEvents', 'ButilSubscription'. Omit the type name to list every public type instead: the injectable services and the static extension classes with a summary each, and the option, handle, event-argument and enum types by name and kind.")]
    public ButilApiDetailsResultDto GetButilApiDetails(string? typeName = null)
    {
        var needle = (typeName ?? string.Empty).Trim();

        // No name at all is a request for the list, not a failed lookup: it is the one call an agent
        // makes when it does not yet know what to ask for, and answering "no type called ''" to it
        // would be technically true and useless.
        if (needle.Length == 0)
        {
            return new ButilApiDetailsResultDto
            {
                Types = ButilApiCatalog.TypeListing,
                Message = "Summaries are listed for the services and the static classes - what a caller picks one of. " +
                          "The option, handle, event-argument and enum types are named here and documented in full by " +
                          "GetButilApiDetails with that name, which is how they are reached anyway: from a signature."
            };
        }

        var details = ButilApiCatalog.GetTypeDetails(needle);

        // Held to the same cap as every document this server hands out - see ButilApiCatalog.Trim.
        if (details is not null) return new ButilApiDetailsResultDto { Details = ButilApiCatalog.Trim(details, DocsPageRenderer.MaxDocumentLength) };

        // Capped: a "did you mean" listing the whole public surface is the client's context window
        // spent on nothing, and the caller who wants all of it asks for it by name above.
        var candidates = ButilApiCatalog.Types
            .Where(t => t.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return new ButilApiDetailsResultDto
        {
            Message = candidates.Length > 0
                ? $"Bit.Butil has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"Bit.Butil has no public type called '{typeName}'. Call GetButilApiDetails with no type name for the full list, or SearchButil to find it by what it does."
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(PlanButilFeature), Title = "What an API, or a set of them, needs from the page",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Reports what using Butil APIs entails beyond their signatures: which engines implement each browser API underneath, what the calling page has to arrange first (HTTPS, a permission prompt, a user gesture), what each returns that has to be disposed, how they behave while the app is prerendering - and, across the whole set, the combined requirements and the ordered checklist for shipping it. Call it before writing the code, not after: these are the mistakes that compile and then silently do nothing. Pass one name for one API or several separated by newlines, commas or semicolons; each may be a service ('Clipboard'), a member ('Geolocation.WatchPosition') or a docs slug ('web-authn').")]
    public ButilFeaturePlanDto PlanButilFeature(string apis)
    {
        var parts = (apis ?? string.Empty).Split(['\n', '\r', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // An argument that named nothing is planned as one nameless API rather than as a plan of
        // none: an empty plan is a checklist with no reason attached, while the nameless entry
        // carries the sentence saying what the argument wants. This is the only tool here whose
        // argument is genuinely required, so it is the only one with nothing else to fall back to.
        return ButilCapabilityCatalog.Plan(parts.Length > 0 ? parts : [string.Empty]);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilDocsPage), Title = "Read one documentation page",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets one page of the Bit.Butil documentation site as Markdown, including its code samples and its API-reference table. Pass a slug, e.g. 'clipboard', 'indexed-db', 'render-modes' or 'troubleshooting'. Omit the slug to get the index of every page instead, which doubles as the browser-support matrix: each API with the services behind it, the engines that implement it and what it demands of the page.")]
    // The token is defaulted only so the slug can be: the SDK injects it either way, and an
    // optional parameter cannot sit in front of a required one.
    public async Task<string> GetButilDocsPage(string? slug = null, CancellationToken cancellationToken = default)
    {
        // No slug is a request for the index, which carries the support matrix with it: an agent
        // choosing between two APIs needs the engines and the preconditions side by side, and that
        // is the same table as the page listing rather than a second thing to go and ask for.
        if (string.IsNullOrWhiteSpace(slug)) return Truncate(ButilIndexes.DocsPages());

        var page = DocsNav.FindByUrl(slug);

        if (page is null)
        {
            var slugs = string.Join(", ", DocsNav.AllLinks.Select(l => l.Url));

            return $"No documentation page has the slug '{slug}'. Available slugs: {slugs}.";
        }

        // The page is rendered by the same component the site serves, so the documentation an agent
        // reads is the documentation a human reads - there is no second copy that could go stale.
        var (rendered, error) = await DocsPageRenderer.RenderCachedMarkdownAsync(htmlRenderer, navigationManager, logger, BaseUri, page, cancellationToken);

        if (rendered is null) return DocsPageRenderer.Unavailable(page, error);

        // The page renders its own <h1>, so only its source is prepended here.
        return Truncate($"Bit.Butil documentation page: /{page.Url}\n\n{rendered}");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilGuideSection), Title = "Read one section of the guide",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets one section of the Bit.Butil reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'The patterns worth knowing', 'Prerendering is safe by default', 'Subscriptions are disposable', 'Trimming and AOT'. Sub-sections are included and heading matching ignores case and punctuation. Omit the heading to get the list of every section instead.")]
    public string GetButilGuideSection(string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return Truncate(ButilIndexes.GuideSections());

        var section = ButilSourceCatalog.GetGuideSection(heading);

        if (section is null)
        {
            var headings = string.Join(", ", ButilSourceCatalog.GuideSections.Select(s => $"'{s.Heading}'"));

            return $"The guide has no section called '{heading}'. Available sections: {headings}.";
        }

        return Truncate(section);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilSourceFile), Title = "Read one working source file",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets one working Bit.Butil source file verbatim - e.g. 'Demo/Client/Pages/ClipboardPage.razor' for a complete page that exercises one browser API end to end. Omit the path to get the list of every file this server can hand out instead: every page of this documentation site, and the minimal hosting samples.")]
    public string GetButilSourceFile(string? path = null)
    {
        var needle = (path ?? string.Empty).Trim();

        if (needle.Length == 0) return Truncate(ButilIndexes.SourceFiles());

        var content = ButilSourceCatalog.GetSourceFile(needle);

        if (content is not null) return Truncate(content);

        var candidates = ButilSourceCatalog.SourceFiles
            .Where(f => f.Path.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Path)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"No source file at '{path}'. Call GetButilSourceFile with no path for the full list.";
    }

    /// <summary>The origin this request arrived on - see <see cref="DocsPageRenderer.BaseUri"/>.</summary>
    private string BaseUri => DocsPageRenderer.BaseUri(httpContextAccessor);

    private static string Truncate(string text) => DocsPageRenderer.Truncate(text);
}
