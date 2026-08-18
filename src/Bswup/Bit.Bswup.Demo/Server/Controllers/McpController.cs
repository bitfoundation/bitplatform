using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Bit.Bswup.Demo.Client;
using Bit.Bswup.Demo.Server.Dtos;
using Bit.Bswup.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bswup.Demo.Server.Controllers;

/// <summary>
/// The Bswup MCP server: the tools an AI agent calls to add offline support, install progress and
/// controlled updates to a Blazor app without guessing at the API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the TypeScript
/// sources compiled into Bit.Bswup's JavaScript bundles, the README, the docs pages rendered by
/// the very site that documents them, and the demo's and samples' source files - so an agent gets
/// what the current version actually does rather than a snapshot someone wrote down. Two of them
/// go further and run an app's own service-worker file through the same rules the shipped worker
/// applies, which is the only way to answer "will this configuration cache that asset?" without
/// deploying it. The same methods are exposed as plain HTTP GET endpoints under /api/mcp/...,
/// which makes each of them inspectable from a browser.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController(HtmlRenderer htmlRenderer, ILogger<McpController> logger) : ControllerBase
{
    // The docs pages are rich enough that a couple of them would otherwise dominate a client's
    // context window; the ones on this site land far below the cap.
    private const int MaxDocumentLength = 40_000;

    // The most asset URLs one AnalyzeBswupAssetCaching call will decide on.
    private const int MaxAnalyzedAssetUrls = 200;

    // The rendered Markdown of every docs page served so far, keyed by slug.
    private static readonly ConcurrentDictionary<string, string> _renderedPages = new(StringComparer.Ordinal);

    private static readonly string PackageVersion =
        typeof(BswupProgress).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BswupProgress).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupOverview), Title = "Bswup: start here",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Start here. Explains what bit Bswup is, how it is wired into a Blazor WebAssembly app, and lists which of the other Bswup tools to call for what.")]
    public string GetBswupOverview()
    {
        var builder = new StringBuilder();

        builder.AppendLine("""
            # bit Bswup

            Bswup is a service-worker layer for Blazor WebAssembly apps. It takes over the app's startup so the
            user watches a real progress bar instead of a blank page, precaches the app's assets so it works
            offline, and stages new versions in the background so an update is a button the user accepts rather
            than a reload that discards their work.

            It is configured in three places, and every tool below maps to one of them:

            - the `<script src="_content/Bit.Bswup/bit-bswup.js" ...>` tag in the host document (page behavior);
            - `wwwroot/service-worker.js` and `service-worker.published.js`, which assign `self.*` settings and
              then import the engine (caching behavior);
            - a handler function on the page - either your own, or the built-in `BswupProgress` component plus
              `bit-bswup.progress.js` (what the user sees).
            """).AppendLine();

        // Which build the answers come from: every tool below reflects THIS package, not a remembered version.
        builder.AppendLine($"_These tools answer from Bit.Bswup {BswupScriptCatalog.Version} (assembly build {PackageVersion}) - the shipped scripts and assembly in front of you, not a remembered release._").AppendLine();

        builder.AppendLine("""
            ---

            ## Which tool to call

            - `SearchBswup` - **the default entry point.** One query across the guide, the docs pages, every
              script attribute and service-worker setting, the lifecycle events, the JavaScript API and the demo's
              sources; each hit carries the exact follow-up call. Reach for it whenever you do not already know
              the section, slug or option name you want.
            - `GetBswupSetupGuide` - the complete wiring for one hosting model ('standalone-wasm' or
              'blazor-web-app'), as the real files of a working project. Start here when adding Bswup to an app.
            - `GetBswupScriptOptions` - every attribute of the `bit-bswup.js` script tag with its real default,
              read off the shipped script.
            - `GetBswupServiceWorkerSettings` / `GetBswupServiceWorkerModes` - every `self.*` setting of the
              service-worker file, and what each `mode` preset expands to.
            - `InspectBswupServiceWorker` - **run this on the service-worker file you write.** It checks the file
              against the settings the shipped worker actually reads: unknown names, settings assigned after the
              `importScripts` line (where they are silently ignored), a `defaultUrl` nothing serves, and more.
            - `AnalyzeBswupAssetCaching` - answers "will this file be cached?" by running concrete asset URLs
              through the include/exclude lists that file produces, built-in patterns included.
            - `GetBswupEvents` - the lifecycle messages a handler receives, with the payload each one carries.
            - `GetBswupJsApi` - the global `BitBswup` object: update checks, storage persistence, skip-waiting and
              the last-resort reset.
            - `GetBswupProgressUI` - the built-in splash: the `BswupProgress` parameters (read off the shipped
              assembly) and the element ids a custom splash has to use.
            - `GetBswupDocsList` / `GetBswupDocsPage` - the documentation site's pages, as Markdown.
            - `GetBswupGuideSections` / `GetBswupGuideSection` - the library's README, one section at a time.
            - `GetBswupSourceFiles` / `GetBswupSourceFile` - real, working source: this site's own service-worker
              and host document, the minimal samples for both hosting models, and the library's TypeScript.

            ## Rules of thumb when configuring Bswup

            - Whatever you put in `service-worker.js`, put in `service-worker.published.js` too. The published
              file is what deployed builds ship; a setting added to only one of them works in development and
              fails in production.
            - Every `self.*` setting must be assigned BEFORE `self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js')`.
              The engine reads them while it is being imported.
            - The Blazor entry script needs `autostart="false"`: Bswup starts Blazor itself, once the install has
              finished.
            - Keep the API out of the worker (`self.serverHandledUrls`), and keep the worker scripts out of the
              cache (they are excluded by default - do not undo that with `ignoreDefaultExclude`).
            - Never cache `service-worker.js` at the HTTP layer. `Cache-Control: no-cache` on it and on
              `bit-bswup.sw.js` is what keeps clients from getting stuck on an old version.
            """);

        return builder.ToString();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBswup), Title = "Search everything about Bswup",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about bit Bswup at once - the README guide, the documentation pages, every script attribute and service-worker setting, the lifecycle events, the JavaScript API and the demo's source files - and returns the best matches, each with the exact follow-up tool call that returns its full text. Use this first whenever you do not already know which page, setting or event holds the answer. Example queries: 'app never picks up new versions', 'cache an external CDN script', 'offline deep link shows home page', 'show a progress bar while installing'.")]
    public BswupSearchHitDto[] SearchBswup(
        [Description("What you are trying to do or what goes wrong, in your own words - e.g. 'cache an external CDN script', 'app never picks up new versions'. Setting, event and attribute names work too.")] string query,
        [Description("How many hits to return. 1-50; anything outside that is clamped.")] int limit = 12)
    {
        return BswupSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupSetupGuide), Title = "Setup guide for a hosting model",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add bit Bswup to a Blazor app for one hosting model, as the real files of a working project: 'standalone-wasm' (wwwroot/index.html is the host document) or 'blazor-web-app' (a Blazor Web App whose server-rendered App.razor hosts an InteractiveWebAssembly client). Call this before writing any setup code - where the splash markup can live, and which assets the client's manifest does NOT list, differ between the two.")]
    public string GetBswupSetupGuide(
        [Description("'standalone-wasm' for an app whose wwwroot/index.html is the host document, or 'blazor-web-app' for a Blazor Web App whose server-rendered Components/App.razor hosts an InteractiveWebAssembly client.")] string hostingModel)
    {
        return BswupSetupGuide.Get(hostingModel)
            ?? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", BswupSetupGuide.HostingModels)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupScriptOptions), Title = "Script tag attributes",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists every attribute of the bit-bswup.js script tag - scope, log, sw, handler, blazorScript, updateInterval, updateOnVisibility, stallTimeout, persistStorage, options - with the default value read off the shipped script, what it does and the caveats. Call it before writing the script tag.")]
    public BswupOptionDto[] GetBswupScriptOptions()
    {
        return BswupScriptCatalog.ScriptOptions;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupServiceWorkerSettings), Title = "Service worker settings",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists every self.* setting an app can assign in its service-worker.js before importing the Bswup engine - the asset include/exclude lists, externalAssets, defaultUrl, the URL routing lists, passive mode, error tolerance, retries, diagnostics and cache versioning - each with its type, default and caveats, plus the built-in asset include/exclude patterns the shipped worker applies.")]
    public BswupServiceWorkerSettingsDto GetBswupServiceWorkerSettings()
    {
        // A named record rather than an anonymous type: this is the shape the tool publishes as its
        // output schema, and an anonymous type has none to publish.
        return new BswupServiceWorkerSettingsDto
        {
            Settings = BswupScriptCatalog.WorkerSettings,
            DefaultAssetsInclude = BswupScriptCatalog.DefaultAssetsInclude,
            DefaultAssetsExclude = BswupScriptCatalog.DefaultAssetsExclude,
            Notes =
            [
                "Every setting must be assigned BEFORE `self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js')` - the engine reads them as it is imported.",
                "Whatever you set here, set in service-worker.published.js as well: that is the file deployed builds ship.",
                "The URL-matching lists (assetsInclude, assetsExclude, prohibitedUrls, serverHandledUrls, serverRenderedUrls) accept a RegExp, used as written, or a string, which is regex-escaped and matched as a literal SUBSTRING of the URL.",
                "An exclude always beats an include. The default excludes keep the service-worker scripts themselves out of the cache; caching those corrupts the update cycle.",
                "Call InspectBswupServiceWorker with your file to have it checked, and AnalyzeBswupAssetCaching to see which assets it will cache.",
            ]
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupServiceWorkerModes), Title = "Service worker mode presets",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists the self.mode presets ('NoPrerender', 'InitialPrerender', 'AlwaysPrerender', 'FullOffline') and exactly which settings each one fills in, read off the shipped service worker. A preset never overrides a setting the file assigns itself.")]
    public BswupModeDto[] GetBswupServiceWorkerModes()
    {
        return BswupScriptCatalog.Modes;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBswupServiceWorker), Title = "Review a service worker file",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Checks a service-worker.js file against the shipped Bswup worker and reports what it will actually do: which self.* settings it assigns, which of those names the worker does not know (a typo that is silently ignored), settings assigned after the importScripts line (where the engine can no longer see them), a missing engine import, a defaultUrl no asset serves, string entries in the URL lists, and what a mode preset adds. Run it on every service-worker file you write or change - none of these failures produce an error anyone sees until a user is offline.")]
    public BswupServiceWorkerInspectionDto InspectBswupServiceWorker(
        [Description("The full content of the service-worker.js (or service-worker.published.js) file to check, verbatim.")] string script)
    {
        return BswupServiceWorkerInspector.Inspect(script);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(AnalyzeBswupAssetCaching), Title = "Will this asset be cached?",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Answers whether specific assets will be cached under a given service-worker.js, by running their URLs through the include/exclude lists that file produces - the shipped built-in patterns first, then the file's own - exactly as the worker builds them. Pass the service-worker file's content and the asset URLs as they appear in service-worker-assets.js (e.g. '_framework/blazor.boot.json', 'css/app.css'), separated by newlines, commas or semicolons. Use it after adding an assetsInclude/assetsExclude pattern, or when an asset is unexpectedly missing offline.")]
    public BswupAssetAnalysisDto AnalyzeBswupAssetCaching(
        [Description("The full content of the service-worker.js file whose include/exclude lists should decide these assets, verbatim.")] string script,
        [Description("The asset URLs to decide, as they appear in service-worker-assets.js (e.g. '_framework/blazor.boot.json', 'css/app.css') - one per line, or separated by commas or semicolons.")] string assetUrls)
    {
        var urls = (assetUrls ?? string.Empty).Split(['\n', '\r', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Every URL is run against every pattern, so an agent that pastes a whole manifest in
        // turns one call into a lot of matching. The cap sits far above the handful of assets a
        // real question is about, and the answer says out loud when it was applied - a silently
        // truncated list would read as 'these are all of them'.
        var analysis = BswupServiceWorkerInspector.AnalyzeAssets(script, urls.Take(MaxAnalyzedAssetUrls));

        if (urls.Length <= MaxAnalyzedAssetUrls) return analysis;

        return analysis with
        {
            Notes = [.. analysis.Notes, $"Only the first {MaxAnalyzedAssetUrls} of the {urls.Length} URLs passed were analyzed; ask again with the rest to cover them."]
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupEvents), Title = "Lifecycle events",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists every lifecycle message Bswup hands to the page's handler function - downloadStarted, downloadProgress, downloadFinished, updateReady, updateFound, updateNotFound, updateCheckFailed, stateChanged, activate, error - with the string each constant resolves to and the shape of the data payload that comes with it. Call it before writing a custom handler.")]
    public BswupEventDto[] GetBswupEvents()
    {
        return BswupScriptCatalog.Events;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupJsApi), Title = "JavaScript API (BitBswup)",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Gets the global BitBswup object the page script installs: checkForUpdate, persistStorage, skipWaiting, forceRefresh and version - with what each one resolves with and when to call it. Use it for a 'check for updates' button, a custom poller or a 'reset app' action.")]
    public BswupJsApiDto[] GetBswupJsApi()
    {
        return BswupScriptCatalog.JsApi;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupProgressUI), Title = "Built-in progress UI reference",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Gets the built-in progress UI reference: every parameter of the BswupProgress component with the default value read off the shipped assembly, the element ids bit-bswup.progress.js drives (what a custom ChildContent splash has to render), the runtime config call, and the script and stylesheet the page needs.")]
    public BswupProgressUiDto GetBswupProgressUI()
    {
        return BswupProgressCatalog.ProgressUi;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupDocsList), Title = "List documentation pages",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists the pages of the bit Bswup documentation site with their descriptions and search keywords. Use it to pick the slug to pass to GetBswupDocsPage.")]
    public BswupDocsPageDto[] GetBswupDocsList()
    {
        return [.. DocsCatalog.Sections.SelectMany(section => section.Pages.Select(page => new BswupDocsPageDto
        {
            Section = section.Title,
            Slug = page.Slug,
            Url = page.Url,
            Title = page.Title,
            Description = page.Description,
            Keywords = page.Keywords
        }))];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupDocsPage), Title = "Read a documentation page",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets one page of the bit Bswup documentation site as Markdown, including its code samples. Pass a slug from GetBswupDocsList, e.g. 'service-worker', 'events' or 'troubleshooting'. Omit it for the introduction.")]
    public async Task<string> GetBswupDocsPage(
        [Description("A slug from GetBswupDocsList, e.g. 'service-worker', 'events', 'troubleshooting'. Omit it for the introduction.")] string? slug = null)
    {
        // The introduction's own slug is the empty string; agents reach for a word instead, and
        // DocsCatalog.FindBySlug maps those words for every caller.
        var page = DocsCatalog.FindBySlug(slug);

        if (page is null)
        {
            var slugs = string.Join(", ", DocsCatalog.AllPages.Select(p => p.Slug.Length == 0 ? "(empty)" : p.Slug));

            return $"No documentation page has the slug '{slug}'. Available slugs: {slugs}.";
        }

        // Rendering a page and flattening it costs far more than serving it; the pages are static,
        // so the first caller pays for it and everyone after reads the same Markdown.
        if (_renderedPages.TryGetValue(page.Slug, out var cached)) return cached;

        // The page is rendered by the same component the site serves, so the documentation an agent
        // reads is the documentation a human reads - there is no second copy that could go stale.
        var (rendered, error) = await DocsPageRenderer.TryRenderMarkdownAsync(htmlRenderer, page, logger);

        // Not cached: a page that failed to render is a bug to be fixed, not a stale answer to keep.
        if (rendered is null) return DocsPageRenderer.Unavailable(page, error);

        // The page renders its own <h1>, so only its source is prepended here.
        var markdown = Truncate($"bit Bswup documentation page: {page.Url}\n\n{rendered}");

        _renderedPages[page.Slug] = markdown;

        return markdown;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupGuideSections), Title = "List reference guide sections",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists every section of the bit Bswup reference guide (the library's README), with its heading and size. Use it to pick the heading to pass to GetBswupGuideSection.")]
    public BswupGuideSectionDto[] GetBswupGuideSections()
    {
        return BswupSourceCatalog.GuideSections;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupGuideSection), Title = "Read a reference guide section",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets one section of the bit Bswup reference guide as Markdown, with its code samples - e.g. 'JavaScript API', 'The built-in progress UI (BswupProgress)', 'Backing out of Bswup (the cleanup worker)', 'Upgrading to v-10-6-0'. Sub-sections are included. Heading matching ignores case and punctuation.")]
    public string GetBswupGuideSection(
        [Description("A heading from GetBswupGuideSections, e.g. 'JavaScript API'. Matching ignores case and punctuation, and sub-sections come with it.")] string heading)
    {
        var section = BswupSourceCatalog.GetGuideSection(heading);

        if (section is null)
        {
            var headings = string.Join(", ", BswupSourceCatalog.GuideSections.Select(s => $"'{s.Heading}'"));

            return $"The guide has no section called '{heading}'. Available sections: {headings}.";
        }

        return Truncate(section);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupSourceFiles), Title = "List available source files",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists the working Bswup source files this server can hand out: this documentation site's own host document and service-worker files, the minimal samples for both hosting models, and the library's own TypeScript sources (the page script, the service worker, the progress UI and the cleanup worker). Use it to pick the path to pass to GetBswupSourceFile.")]
    public BswupSourceFileDto[] GetBswupSourceFiles()
    {
        return BswupSourceCatalog.SourceFiles;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupSourceFile), Title = "Read a source file",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets one source file listed by GetBswupSourceFiles, verbatim - e.g. 'Demo/Client/wwwroot/service-worker.published.js' for the configuration of a deployed Blazor Web App, 'Sample/BasicSample/wwwroot/index.html' for a complete hand-written splash and handler, or 'Library/Scripts/bit-bswup.sw.ts' for the engine itself.")]
    public string GetBswupSourceFile(
        [Description("A path from GetBswupSourceFiles, e.g. 'Demo/Client/wwwroot/service-worker.published.js' or 'Library/Scripts/bit-bswup.sw.ts'.")] string path)
    {
        var content = BswupSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BswupSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBswupSourceFiles for the full list.";
        }

        return Truncate(content);
    }

    private static string Truncate(string text)
    {
        return text.Length <= MaxDocumentLength
            ? text
            : $"{text[..MaxDocumentLength]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }
}
