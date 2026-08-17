using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Concurrent;
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
/// </summary>
[ApiController]
[McpServerToolType]
// Fully qualified: Microsoft.AspNetCore.Components brings its own RouteAttribute, and this file
// needs that namespace for the renderer and the NavigationManager.
[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
public class McpController(HtmlRenderer htmlRenderer, NavigationManager navigationManager, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    // The docs pages are rich enough that a couple of them would otherwise dominate a client's
    // context window; the ones on this site land far below the cap.
    private const int MaxDocumentLength = 40_000;

    // The rendered Markdown of every docs page served so far, keyed by slug.
    private static readonly ConcurrentDictionary<string, string> _renderedPages = new(StringComparer.Ordinal);

    private static readonly string ButilVersion =
        typeof(BitButil).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BitButil).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilOverview))]
    [Description("Start here. Explains what Bit.Butil is, how to install and register it, shows a minimal working page, and lists which of the other Butil tools to call for what.")]
    public string GetButilOverview()
    {
        var builder = new StringBuilder();

        var readme = ButilSourceCatalog.Readme;
        var firstSection = readme.IndexOf("\n## ", StringComparison.Ordinal);
        builder.AppendLine(firstSection > 0 ? readme[..firstSection].Trim() : readme).AppendLine();

        // Which build the answers come from: every tool below reflects THIS assembly, not a remembered version.
        builder.AppendLine($"_These tools answer from Bit.Butil {ButilVersion}, loaded in this server, " +
                           $"and cover {ButilApiCatalog.Services.Length} injectable services across " +
                           $"{DocsNav.ApiLinks.Count()} documented browser APIs._").AppendLine();

        AppendGuideSection(builder, "Getting started");
        AppendGuideSection(builder, "The patterns worth knowing");

        builder.AppendLine("""
            ---

            ## Which tool to call

            - `SearchButil` - **the default entry point.** One query across the guide, the docs pages, every public
              type and member, the browser-support matrix and the demo's sources; each hit carries the exact
              follow-up call. Reach for it whenever you do not already know the service, page or member you want -
              which is most of the time, because the name a task suggests is rarely the name the web platform chose.
            - `GetButilSetupGuide` - the complete wiring for one Blazor hosting model ('wasm', 'web-app', 'server',
              'hybrid'), as the real files of a working project. Start here when adding Butil to an app.
            - `GetButilApiList` / `GetButilApiDetails` - the exact public API: every service, every method with its
              full signature, every option type and enum, straight out of the shipped assembly with the XML
              documentation that ships with it. Call this before writing code against a member you are not sure
              about - the wrappers follow the browser API's own naming, not an intuitive one.
            - `InspectButilApi` - what using one API entails beyond its signatures: which engines implement it, what
              the page has to arrange first (HTTPS, a permission prompt, a click), what has to be disposed, and how
              it behaves while the app is prerendering. This is where the bugs that compile come from.
            - `PlanButilFeature` - the same, for the whole set of APIs a feature needs at once: the strictest
              hosting requirement any of them imposes, the engines that will run all of them, and the checklist.
            - `GetButilBrowserSupport` - the full matrix in one call: every documented API with its engine coverage
              and its preconditions.
            - `GetButilDocsList` / `GetButilDocsPage` - the documentation site's pages: one per browser API, with
              runnable samples, an API-reference table and the caveats that matter for that API.
            - `GetButilGuideSections` / `GetButilGuideSection` - the library's own reference guide (its README), one
              topic at a time.
            - `GetButilSourceFiles` / `GetButilSourceFile` - real, working source: every page of this site, and the
              minimal hosting samples for standalone WebAssembly and for Hybrid.

            ## Rules of thumb when writing Butil code

            - Register the services with `AddBitButilServices()` in EVERY DI container that renders your components -
              in a Blazor Web App with an interactive client that means both the server and the client project.
            - Add `<script src="_content/Bit.Butil/bit-butil.js"></script>` to the host page BEFORE the Blazor script.
            - Inject a wrapper by its own name: `@inject Bit.Butil.Clipboard clipboard`.
            - Touch the browser from `OnAfterRenderAsync` or from an event handler, never from `OnInitializedAsync`.
              Under prerendering there is no JS runtime: reads return safe defaults and void calls are no-ops, so the
              code does not crash - it silently does nothing.
            - Dispose what you open. Every subscription returns a `ButilSubscription`, and a handle
              (`MediaStreamHandle`, `MediaRecordingHandle`, a File System or WakeLock handle) holds real hardware
              until it is disposed.
            - Where the browser can refuse - a denied permission, a dismissed picker - the wrapper returns
              `false`/`null` rather than throwing. Treat it as a branch, not as an error.
            """);

        return builder.ToString();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(SearchButil))]
    [Description("Searches everything known about Bit.Butil at once - the reference guide, the documentation pages, every public type and member, the browser-support matrix and the demo's source files - and returns the best matches, each with the exact follow-up tool call that returns its full text. Use this first whenever you do not already know which service or member does the job. Example queries: 'copy text to clipboard', 'keep the screen awake', 'detect when an element scrolls into view', 'store data offline', 'read a file the user picked'.")]
    public ButilSearchHitDto[] SearchButil(string query, int limit = 12)
    {
        return ButilSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilSetupGuide))]
    [Description("Gets the complete wiring needed to add Bit.Butil to a Blazor app in one hosting model, as the real files of a working project: 'wasm' (standalone Blazor WebAssembly), 'web-app' (Blazor Web App with prerendering), 'server' (Blazor Server) or 'hybrid' (MAUI/WPF/WinForms). Call this before writing any setup code - which host page carries the script tag and how many DI containers register the services differ per hosting model, and getting either wrong produces an app where every browser call silently does nothing.")]
    public string GetButilSetupGuide(string hostingModel)
    {
        return ButilSetupGuide.Get(hostingModel)
            ?? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", ButilSetupGuide.HostingModels)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilApiList))]
    [Description("Lists every public type of the Bit.Butil library - the injectable services, the static extension classes, the option types, enums and event/key-code catalogs - with its kind and summary. The 'IsInjectable' ones are the classes you inject by their own name. Use it to pick the type to pass to GetButilApiDetails.")]
    public ButilApiTypeDto[] GetButilApiList()
    {
        return ButilApiCatalog.Types;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilApiDetails))]
    [Description("Gets the full reference of one Bit.Butil type: every method with its complete signature and default parameter values, every property, event or enum value, each with the XML documentation that ships with the library. Call it before using a member you are unsure about, e.g. 'Clipboard', 'LocalStorage', 'Geolocation', 'ElementReferenceExtensions', 'ButilEvents', 'ButilSubscription'.")]
    public ButilApiDetailsResultDto GetButilApiDetails(string typeName)
    {
        var details = ButilApiCatalog.GetTypeDetails(typeName);

        if (details is not null) return new ButilApiDetailsResultDto { Details = details };

        var candidates = ButilApiCatalog.Types
            .Where(t => t.Name.Contains(typeName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .ToArray();

        return new ButilApiDetailsResultDto
        {
            Message = candidates.Length > 0
                ? $"Bit.Butil has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"Bit.Butil has no public type called '{typeName}'. Call GetButilApiList for the full list, or SearchButil to find it by what it does."
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectButilApi))]
    [Description("Reports what using one Butil API entails beyond its signatures: which engines implement the browser API underneath it, what the calling page has to arrange first (HTTPS, a permission prompt, a user gesture), what it returns that has to be disposed, and how it behaves while the app is prerendering. Call it before writing the code, not after: these are the mistakes that compile and then silently do nothing. Accepts a service name ('Clipboard'), a member ('Geolocation.WatchPosition'), or a docs slug ('web-authn').")]
    public ButilApiInspectionDto InspectButilApi(string name)
    {
        return ButilCapabilityCatalog.Inspect(name);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(PlanButilFeature))]
    [Description("Takes the set of Butil APIs a feature needs and reports their combined consequences: whether the app now has to be served over HTTPS, which permission prompts the UI has to explain, which calls must start from a click, which engines will run all of them, what has to be disposed - and the ordered checklist for shipping it. Pass the API or service names separated by newlines, commas or semicolons, e.g. 'MediaDevices, MediaRecorder, FileSystem'.")]
    public ButilFeaturePlanDto PlanButilFeature(string apis)
    {
        var parts = (apis ?? string.Empty).Split(['\n', '\r', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return ButilCapabilityCatalog.Plan(parts);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilBrowserSupport))]
    [Description("Lists every browser API Bit.Butil wraps with the engines that implement it and the preconditions it imposes on the page - a secure context, a permission prompt, a user gesture, an experimental flag. Use it to choose between two APIs that would both work, or to find out up front what a feature will demand of the deployment.")]
    public ButilCapabilityDto[] GetButilBrowserSupport()
    {
        return ButilCapabilityCatalog.Capabilities;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilDocsList))]
    [Description("Lists the pages of the Bit.Butil documentation site - one per browser API, plus the guide pages - with their summaries, the services they document and their browser support. Use it to pick the slug to pass to GetButilDocsPage.")]
    public ButilDocsPageDto[] GetButilDocsList()
    {
        return [.. DocsNav.Groups.SelectMany(group => group.Links.Select(link => new ButilDocsPageDto
        {
            Group = group.Title,
            Slug = link.Url,
            Url = $"/{link.Url}",
            Title = link.Title,
            Summary = link.Summary,
            Services = link.TypeNames(),
            BrowserSupport = link.Support.Label(),
            Requires = link.Needs.Labels()
        }))];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilDocsPage))]
    [Description("Gets one page of the Bit.Butil documentation site as Markdown, including its code samples and its API-reference table. Pass a slug from GetButilDocsList, e.g. 'clipboard', 'indexed-db', 'render-modes' or 'troubleshooting'.")]
    public async Task<string> GetButilDocsPage(string slug)
    {
        var page = DocsNav.FindByUrl(slug);

        if (page is null)
        {
            var slugs = string.Join(", ", DocsNav.AllLinks.Select(l => l.Url));

            return $"No documentation page has the slug '{slug}'. Available slugs: {slugs}.";
        }

        // Rendering a page and flattening it costs far more than serving it; the pages are static,
        // so the first caller pays for it and everyone after reads the same Markdown.
        if (_renderedPages.TryGetValue(page.Url, out var cached)) return cached;

        // The page is rendered by the same component the site serves, so the documentation an agent
        // reads is the documentation a human reads - there is no second copy that could go stale.
        var (rendered, error) = await DocsPageRenderer.TryRenderMarkdownAsync(htmlRenderer, navigationManager, BaseUri, page);

        // Not cached: a page that failed to render is a bug to be fixed, not a stale answer to keep.
        if (rendered is null) return DocsPageRenderer.Unavailable(page, error);

        // The page renders its own <h1>, so only its source is prepended here.
        var markdown = Truncate($"Bit.Butil documentation page: /{page.Url}\n\n{rendered}");

        _renderedPages[page.Url] = markdown;

        return markdown;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilGuideSections))]
    [Description("Lists every section of the Bit.Butil reference guide (the library's README), with its heading and size. Use it to pick the heading to pass to GetButilGuideSection.")]
    public ButilGuideSectionDto[] GetButilGuideSections()
    {
        return ButilSourceCatalog.GuideSections;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilGuideSection))]
    [Description("Gets one section of the Bit.Butil reference guide as Markdown, with its code samples - e.g. 'Getting started', 'Prerendering is safe by default', 'Subscriptions are disposable', 'Trimming and AOT'. Sub-sections are included. Heading matching ignores case and punctuation.")]
    public string GetButilGuideSection(string heading)
    {
        var section = ButilSourceCatalog.GetGuideSection(heading);

        if (section is null)
        {
            var headings = string.Join(", ", ButilSourceCatalog.GuideSections.Select(s => $"'{s.Heading}'"));

            return $"The guide has no section called '{heading}'. Available sections: {headings}.";
        }

        return Truncate(section);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilSourceFiles))]
    [Description("Lists the working Bit.Butil source files this server can hand out: every page of this documentation site (one per browser API, each a complete working example), and the minimal hosting samples. Use it to pick the path to pass to GetButilSourceFile.")]
    public ButilSourceFileDto[] GetButilSourceFiles()
    {
        return ButilSourceCatalog.SourceFiles;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetButilSourceFile))]
    [Description("Gets one source file listed by GetButilSourceFiles, verbatim - e.g. 'Demo/Client/Pages/ClipboardPage.razor' for a complete, working page that exercises one browser API end to end.")]
    public string GetButilSourceFile(string path)
    {
        var content = ButilSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = ButilSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetButilSourceFiles for the full list.";
        }

        return Truncate(content);
    }

    /// <summary>
    /// The origin this request arrived on - what a docs page's canonical URL and anchors are built
    /// from while it renders. Behind a reverse proxy the forwarded-headers middleware has already
    /// rewritten Scheme and Host, so this is the public origin rather than the container's.
    /// <para>
    /// Read off the accessor rather than off <c>ControllerBase.Request</c>: over MCP these methods
    /// are invoked on an instance the tool host built from DI, which never had a ControllerContext
    /// assigned, so <c>Request</c> would be a null reference on exactly the transport that matters.
    /// </para>
    /// </summary>
    private string BaseUri => httpContextAccessor.HttpContext is { Request: var request }
        ? $"{request.Scheme}://{request.Host}/"
        : "https://localhost/";

    /// <summary>
    /// A renamed README heading must not silently leave a blank gap in the overview - the agent is
    /// told where the text went instead.
    /// </summary>
    private static void AppendGuideSection(StringBuilder builder, string heading)
    {
        builder.AppendLine(ButilSourceCatalog.GetGuideSection(heading)
                           ?? $"_The guide's \"{heading}\" section was not found in this build. " +
                              $"Call GetButilGuideSections for the sections it does have._")
               .AppendLine();
    }

    private static string Truncate(string text)
    {
        return text.Length <= MaxDocumentLength
            ? text
            : $"{text[..MaxDocumentLength]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }
}
